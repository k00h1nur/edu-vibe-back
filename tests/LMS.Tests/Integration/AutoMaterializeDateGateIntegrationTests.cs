using FluentAssertions;
using LMS.Application.Common.Abstractions;
using LMS.Application.Features.Sessions;
using LMS.Application.Features.TaskSubmissions;
using LMS.Application.Features.Tasks;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using LMS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LMS.Tests.Integration;

/// <summary>
/// Real-DB integration tests for the F3↔F4 guarantees that pure cores can't prove:
/// the student submit date-gate (security — surprise #3), the staff bypass, and the
/// reschedule re-run leaving zero duplicate tasks / zero orphaned assignments.
/// Self-skip when no Postgres is reachable (see <see cref="IntegrationDb"/>).
/// </summary>
public sealed class AutoMaterializeDateGateIntegrationTests : IClassFixture<IntegrationDb>
{
    private readonly IntegrationDb _db;
    public AutoMaterializeDateGateIntegrationTests(IntegrationDb db) => _db = db;

    private static DateOnly Future => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(5);
    private static DateOnly Past => DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-3);

    // ---- 1. Submit date-gate (the surprise-#3 security proof) -----------------
    [SkippableFact]
    public async Task Submit_is_forbidden_before_the_lesson_date_and_allowed_on_or_after()
    {
        Skip.IfNot(_db.Available, _db.SkipReason);

        Guid studentProfileId, futureTaskId, pastTaskId;
        await using (var ctx = _db.CreateContext())
        {
            var teacher = new User($"t{Guid.NewGuid():N}@x.io", "hash");
            var studentUser = new User($"s{Guid.NewGuid():N}@x.io", "hash");
            await ctx.Users.AddRangeAsync(teacher, studentUser);
            await ctx.SaveChangesAsync();

            var student = new StudentProfile(studentUser.Id, studentUser);
            var cls = new Class("Gate class", 20, (Modality)1);
            await ctx.StudentProfiles.AddAsync(student);
            await ctx.Classes.AddAsync(cls);
            await ctx.SaveChangesAsync();

            futureTaskId = await SeedLessonWithTask(ctx, cls.Id, teacher, Future);
            pastTaskId = await SeedLessonWithTask(ctx, cls.Id, teacher, Past);
            studentProfileId = student.Id;
        }

        // Before the lesson date → FORBIDDEN (can't read or submit via the endpoint).
        await using (var ctx = _db.CreateContext())
        {
            var handler = new TaskSubmissionsHandlers(ctx, new ManualGrader(),
                Student(studentProfileId));
            var r = await handler.Handle(new SubmitTaskResponseCommand(futureTaskId, studentProfileId, "{}"), default);
            r.Success.Should().BeFalse();
            r.ErrorCode.Should().Be("FORBIDDEN");
        }

        // On/after the lesson date → submission succeeds.
        await using (var ctx = _db.CreateContext())
        {
            var handler = new TaskSubmissionsHandlers(ctx, new ManualGrader(),
                Student(studentProfileId));
            var r = await handler.Handle(new SubmitTaskResponseCommand(pastTaskId, studentProfileId, "{}"), default);
            r.Success.Should().BeTrue();
        }
    }

    // ---- 2. Read gate: students date-gated, staff never -----------------------
    [SkippableFact]
    public async Task Task_read_is_date_gated_for_students_but_never_for_staff()
    {
        Skip.IfNot(_db.Available, _db.SkipReason);

        Guid assignmentId;
        await using (var ctx = _db.CreateContext())
        {
            var teacher = new User($"t{Guid.NewGuid():N}@x.io", "hash");
            await ctx.Users.AddAsync(teacher);
            await ctx.SaveChangesAsync();
            var cls = new Class("Read class", 20, (Modality)1);
            await ctx.Classes.AddAsync(cls);
            await ctx.SaveChangesAsync();
            assignmentId = await SeedLessonWithAssignment(ctx, cls.Id, teacher, Future);
        }

        await using (var ctx = _db.CreateContext())
        {
            var asStudent = new TasksHandlers(ctx, Student(Guid.NewGuid()));
            var student = await asStudent.Handle(new GetAssignmentTasksQuery(assignmentId), default);
            student.Success.Should().BeFalse();
            student.ErrorCode.Should().Be("FORBIDDEN");

            var asStaff = new TasksHandlers(ctx, Staff());
            var staff = await asStaff.Handle(new GetAssignmentTasksQuery(assignmentId), default);
            staff.Success.Should().BeTrue();
            staff.Data.Should().NotBeNull();
        }
    }

    // ---- 3. Reschedule re-run: no duplicate tasks, no orphaned assignment ------
    [SkippableFact]
    public async Task Reschedule_rerun_does_not_duplicate_tasks_or_orphan_the_assignment()
    {
        Skip.IfNot(_db.Available, _db.SkipReason);

        Guid classId, teacherId, lessonId;
        await using (var ctx = _db.CreateContext())
        {
            var teacher = new User($"t{Guid.NewGuid():N}@x.io", "hash");
            await ctx.Users.AddAsync(teacher);
            await ctx.SaveChangesAsync();

            var template = new CurriculumTemplate("RC tmpl", (CurriculumCategory)1, null, "d", isSystem: false);
            await ctx.CurriculumTemplates.AddAsync(template);
            await ctx.SaveChangesAsync();
            var module = new CurriculumModule(template.Id, 1, "M");
            await ctx.CurriculumModules.AddAsync(module);
            await ctx.SaveChangesAsync();
            var unit = new CurriculumUnit(module.Id, 1, "U");
            await ctx.CurriculumUnits.AddAsync(unit);
            await ctx.SaveChangesAsync();
            var lesson = new CurriculumLesson(unit.Id, 1, "L", null, "hw", "mat", false);
            await ctx.CurriculumLessons.AddAsync(lesson);
            await ctx.SaveChangesAsync();
            await ctx.LessonDefaultTasks.AddAsync(
                new LessonDefaultTask(lesson.Id, 0, LearningTaskType.MultipleChoice, "Q", 1, "{}"));

            var cls = new Class("RC class", 20, (Modality)1);
            cls.SetCurriculumTemplate(template.Id);
            await ctx.Classes.AddAsync(cls);
            await ctx.SaveChangesAsync();

            classId = cls.Id; teacherId = teacher.Id; lessonId = lesson.Id;
        }

        var cmd = new ApplyClassScheduleCommand(
            classId, SchedulePatternType.Custom, 127,
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(13),
            new TimeOnly(9, 0), new TimeOnly(10, 0), null, null);

        // Run #1: generate sessions, link one to the lesson, materialize it.
        Guid homeworkSessionId;
        await using (var ctx = _db.CreateContext())
        {
            (await new SessionsHandlers(ctx, Admin()).Handle(cmd, default)).Success.Should().BeTrue();
        }
        await using (var ctx = _db.CreateContext())
        {
            var session = await ctx.ClassSessions
                .Where(s => s.ClassId == classId).OrderBy(s => s.SessionDate).FirstAsync();
            session.LinkCurriculumLesson(lessonId);
            await ctx.SaveChangesAsync();
            homeworkSessionId = session.Id;

            var outcome = await new LessonTaskMaterializer(ctx).MaterializeAsync(homeworkSessionId, teacherId, default);
            outcome.CreatedTasks.Should().Be(1);
        }

        // Run #2: same schedule. The homework'd session must survive; re-materialise = 0.
        await using (var ctx = _db.CreateContext())
        {
            (await new SessionsHandlers(ctx, Admin()).Handle(cmd, default)).Success.Should().BeTrue();
        }
        await using (var ctx = _db.CreateContext())
        {
            (await ctx.ClassSessions.AnyAsync(s => s.Id == homeworkSessionId))
                .Should().BeTrue("the homework'd session is preserved across a reschedule");

            var outcome = await new LessonTaskMaterializer(ctx).MaterializeAsync(homeworkSessionId, teacherId, default);
            outcome.CreatedTasks.Should().Be(0, "re-running materialise creates no duplicates");
        }

        // No duplication, no orphan: exactly one assignment + one task, assignment still on a real session.
        await using (var ctx = _db.CreateContext())
        {
            var assignments = await ctx.Assignments.Where(a => a.ClassId == classId).ToListAsync();
            assignments.Should().ContainSingle();
            var asn = assignments[0];
            (await ctx.LearningTasks.CountAsync(t => t.AssignmentId == asn.Id)).Should().Be(1);
            asn.ClassSessionId.Should().Be(homeworkSessionId);
            (await ctx.ClassSessions.AnyAsync(s => s.Id == asn.ClassSessionId)).Should().BeTrue("no orphaned assignment");
        }
    }

    // ---- helpers --------------------------------------------------------------
    private static async Task<Guid> SeedLessonWithTask(LMSDbContext ctx, Guid classId, User teacher, DateOnly date)
    {
        var session = new ClassSession(classId, date, new TimeOnly(9, 0), new TimeOnly(10, 0));
        await ctx.ClassSessions.AddAsync(session);
        await ctx.SaveChangesAsync();
        var assignment = new Assignment(classId, "HW", teacher);
        assignment.SetSession(session.Id);
        await ctx.Assignments.AddAsync(assignment);
        await ctx.SaveChangesAsync();
        var task = new LearningTask(assignment.Id, 0, LearningTaskType.MultipleChoice, "Q", 1, "{}");
        await ctx.LearningTasks.AddAsync(task);
        await ctx.SaveChangesAsync();
        return task.Id;
    }

    private static async Task<Guid> SeedLessonWithAssignment(LMSDbContext ctx, Guid classId, User teacher, DateOnly date)
    {
        var session = new ClassSession(classId, date, new TimeOnly(9, 0), new TimeOnly(10, 0));
        await ctx.ClassSessions.AddAsync(session);
        await ctx.SaveChangesAsync();
        var assignment = new Assignment(classId, "HW", teacher);
        assignment.SetSession(session.Id);
        await ctx.Assignments.AddAsync(assignment);
        await ctx.SaveChangesAsync();
        await ctx.LearningTasks.AddAsync(new LearningTask(assignment.Id, 0, LearningTaskType.MultipleChoice, "Q", 1, "{}"));
        await ctx.SaveChangesAsync();
        return assignment.Id;
    }

    private static FakeUser Student(Guid studentProfileId) =>
        new() { UserId = Guid.NewGuid(), StudentProfileId = studentProfileId };

    private static FakeUser Staff() =>
        new() { UserId = Guid.NewGuid(), StaffProfileId = Guid.NewGuid(), Roles = new[] { "Teacher" } };

    private static FakeUser Admin() =>
        new() { UserId = Guid.NewGuid(), StaffProfileId = Guid.NewGuid(), Roles = new[] { "Admin" } };

    private sealed class FakeUser : ICurrentUserService
    {
        public Guid? UserId { get; init; }
        public string? Email { get; init; }
        public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
        public Guid? StudentProfileId { get; init; }
        public Guid? StaffProfileId { get; init; }
        public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class ManualGrader : ITaskGrader
    {
        public GradeResult Grade(LearningTask task, string responseJson) => GradeResult.RequiresManualReview;
    }
}
