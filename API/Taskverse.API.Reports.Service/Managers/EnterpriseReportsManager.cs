using Microsoft.EntityFrameworkCore;
using Taskverse.API.Reports.Service.Models;
using Taskverse.Data.DataAccess;

namespace Taskverse.API.Reports.Service.Managers;

public class EnterpriseReportsManager : IEnterpriseReportsManager
{
    private readonly TaskverseContext _context;

    public EnterpriseReportsManager(TaskverseContext context)
    {
        _context = context;
    }

    public async Task ExecuteRawSqlAsync(string sql, CancellationToken ct)
    {
        await _context.Database.ExecuteSqlRawAsync(sql, ct);
    }

    public async Task<List<FilterOptionResponse>> GetCollegesAsync(CancellationToken ct)
    {
        return await _context.Colleges.AsNoTracking()
            .Where(c => c.Status == "Active")
            .OrderBy(c => c.CollegeName)
            .Select(c => new FilterOptionResponse { Id = c.CollegeId.ToString(), Name = c.CollegeName ?? "Unnamed" })
            .ToListAsync(ct);
    }

    public async Task<List<FilterOptionResponse>> GetBranchesAsync(Guid? collegeId, CancellationToken ct)
    {
        var query = _context.Classes.AsNoTracking().AsQueryable();
        if (collegeId.HasValue)
            query = query.Where(c => c.CollegeId == collegeId.Value);

        return await query.OrderBy(c => c.Name)
            .Select(c => new FilterOptionResponse { Id = c.ClassId.ToString(), Name = c.Name })
            .ToListAsync(ct);
    }

    public async Task<List<FilterOptionResponse>> GetBatchesAsync(Guid? classId, CancellationToken ct)
    {
        var query = _context.Batches.AsNoTracking().AsQueryable();
        if (classId.HasValue)
            query = query.Where(b => b.ClassId == classId.Value);

        return await query.OrderBy(b => b.Name)
            .Select(b => new FilterOptionResponse { Id = b.BatchId.ToString(), Name = b.Name })
            .ToListAsync(ct);
    }

    public async Task<List<FilterOptionResponse>> GetTrainersAsync(Guid? collegeId, CancellationToken ct)
    {
        var query = _context.Trainers.AsNoTracking().AsQueryable();
        if (collegeId.HasValue)
            query = query.Where(t => t.CollegeId == collegeId.Value);

        return await query.OrderBy(t => t.FullName)
            .Select(t => new FilterOptionResponse { Id = t.TrainerId.ToString(), Name = t.FullName })
            .ToListAsync(ct);
    }

    public async Task<List<CollegeWiseRowResponse>> BuildCollegeWiseRowsAsync(
        Guid? collegeId, DateTime? dateFrom, DateTime? dateTo, string? academicYear, CancellationToken ct)
    {
        var collegesQuery = _context.Colleges.AsNoTracking().Where(c => c.Status == "Active");
        if (collegeId.HasValue)
            collegesQuery = collegesQuery.Where(c => c.CollegeId == collegeId.Value);

        var colleges = await collegesQuery.OrderBy(c => c.CollegeName).ToListAsync(ct);
        var result = new List<CollegeWiseRowResponse>();

        foreach (var college in colleges)
        {
            var students = await _context.Students.AsNoTracking()
                .Where(s => s.CollegeId == college.CollegeId).ToListAsync(ct);
            var trainers = await _context.Trainers.AsNoTracking()
                .Where(t => t.CollegeId == college.CollegeId).ToListAsync(ct);

            var assessments = _context.Assessments.AsNoTracking()
                .Where(a => a.CollegeId == college.CollegeId);

            if (dateFrom.HasValue)
                assessments = assessments.Where(a => a.CreatedAt >= dateFrom.Value);
            if (dateTo.HasValue)
                assessments = assessments.Where(a => a.CreatedAt <= dateTo.Value);

            var assessmentList = await assessments.ToListAsync(ct);

            var studentIds = students.Select(s => s.StudentId).ToHashSet();
            var results = await _context.Results.AsNoTracking()
                .Where(r => studentIds.Contains(r.StudentId))
                .ToListAsync(ct);

            if (dateFrom.HasValue)
                results = results.Where(r => r.GeneratedAt >= dateFrom.Value).ToList();
            if (dateTo.HasValue)
                results = results.Where(r => r.GeneratedAt <= dateTo.Value).ToList();

            var activeStudents = students.Count(s => s.Status == Data.Enums.UserStatus.APPROVED);
            var avgScore = results.Count > 0 ? results.Average(r => r.Percentage) : 0m;

            result.Add(new CollegeWiseRowResponse
            {
                CollegeName = college.CollegeName ?? "Unnamed",
                TotalStudents = students.Count,
                TotalTrainers = trainers.Count,
                TotalAssessments = assessmentList.Count,
                AssessmentsCompleted = assessmentList.Count(a => a.AssessmentStatus == Data.Enums.AssessmentStatus.Completed),
                AverageScore = Math.Round(avgScore, 2),
                HighestScore = results.Count > 0 ? Math.Round(results.Max(r => r.Percentage), 2) : 0m,
                LowestScore = results.Count > 0 ? Math.Round(results.Min(r => r.Percentage), 2) : 0m,
                PassPercentage = results.Count > 0 ? Math.Round(100m * results.Count(r => r.Percentage >= 50) / results.Count, 2) : 0m,
                ActiveStudents = activeStudents,
                PerformanceGrade = GetGrade(avgScore)
            });
        }
        return result;
    }

    public async Task<List<BranchWiseRowResponse>> BuildBranchWiseRowsAsync(
        Guid? collegeId, Guid? classId, Guid? batchId, DateTime? dateFrom, DateTime? dateTo, CancellationToken ct)
    {
        var classesQuery = _context.Classes.AsNoTracking().AsQueryable();
        if (collegeId.HasValue)
            classesQuery = classesQuery.Where(c => c.CollegeId == collegeId.Value);
        if (classId.HasValue)
            classesQuery = classesQuery.Where(c => c.ClassId == classId.Value);

        var classes = await classesQuery.OrderBy(c => c.Name).ToListAsync(ct);
        var result = new List<BranchWiseRowResponse>();

        foreach (var cls in classes)
        {
            var studentsQuery = _context.Students.AsNoTracking().Where(s => s.ClassId == cls.ClassId);
            if (batchId.HasValue)
                studentsQuery = studentsQuery.Where(s => s.BatchId == batchId.Value);
            var students = await studentsQuery.ToListAsync(ct);

            var trainerCount = await _context.TrainerClasses.AsNoTracking()
                .Where(tc => tc.ClassId == cls.ClassId).CountAsync(ct);

            var studentIds = students.Select(s => s.StudentId).ToHashSet();
            var results = await _context.Results.AsNoTracking()
                .Where(r => studentIds.Contains(r.StudentId))
                .ToListAsync(ct);

            if (dateFrom.HasValue)
                results = results.Where(r => r.GeneratedAt >= dateFrom.Value).ToList();
            if (dateTo.HasValue)
                results = results.Where(r => r.GeneratedAt <= dateTo.Value).ToList();

            var assessmentIds = results.Select(r => r.AssessmentId).Distinct().ToHashSet();

            var assessments = await _context.Assessments.AsNoTracking()
                .Where(a => assessmentIds.Contains(a.AssessmentId))
                .Include(a => a.Subject)
                .Include(a => a.Topic)
                .ToListAsync(ct);

            var topicScores = new Dictionary<string, List<decimal>>();
            foreach (var r in results)
            {
                var assessment = assessments.FirstOrDefault(a => a.AssessmentId == r.AssessmentId);
                var topicName = assessment?.Topic?.TopicName ?? assessment?.Subject?.SubjectName ?? "General";
                if (!topicScores.ContainsKey(topicName))
                    topicScores[topicName] = new List<decimal>();
                topicScores[topicName].Add(r.Percentage);
            }

            var rankedTopics = topicScores
                .Select(kv => new { Topic = kv.Key, Avg = kv.Value.Average() })
                .OrderByDescending(x => x.Avg).ToList();

            result.Add(new BranchWiseRowResponse
            {
                BranchName = cls.Name,
                TotalStudents = students.Count,
                TotalTrainers = trainerCount,
                TotalAssessments = assessmentIds.Count,
                AverageMarks = results.Count > 0 ? Math.Round(results.Average(r => r.Percentage), 2) : 0m,
                HighestMarks = results.Count > 0 ? Math.Round(results.Max(r => r.Percentage), 2) : 0m,
                LowestMarks = results.Count > 0 ? Math.Round(results.Min(r => r.Percentage), 2) : 0m,
                PassPercentage = results.Count > 0 ? Math.Round(100m * results.Count(r => r.Percentage >= 50) / results.Count, 2) : 0m,
                StrongestTopics = rankedTopics.Take(3).Select(x => x.Topic).ToList(),
                WeakestTopics = rankedTopics.TakeLast(3).Select(x => x.Topic).ToList()
            });
        }
        return result;
    }

    public async Task<List<StudentPerformanceRowResponse>> BuildStudentPerformanceRowsAsync(
        Guid? collegeId, Guid? classId, Guid? batchId, Guid? studentId,
        Guid? trainerId, Guid? assessmentId, DateTime? dateFrom, DateTime? dateTo,
        string? performanceLevel, CancellationToken ct)
    {
        var studentsQuery = _context.Students.AsNoTracking()
            .Include(s => s.College)
            .Include(s => s.Class)
            .Include(s => s.Batch)
            .AsQueryable();

        if (collegeId.HasValue)
            studentsQuery = studentsQuery.Where(s => s.CollegeId == collegeId.Value);
        if (classId.HasValue)
            studentsQuery = studentsQuery.Where(s => s.ClassId == classId.Value);
        if (batchId.HasValue)
            studentsQuery = studentsQuery.Where(s => s.BatchId == batchId.Value);
        if (studentId.HasValue)
            studentsQuery = studentsQuery.Where(s => s.StudentId == studentId.Value);

        if (trainerId.HasValue)
        {
            var trainerBatchIds = await _context.TrainerBatches.AsNoTracking()
                .Where(tb => tb.TrainerId == trainerId.Value)
                .Select(tb => tb.BatchId)
                .ToListAsync(ct);
            var trainerClassIds = await _context.TrainerClasses.AsNoTracking()
                .Where(tc => tc.TrainerId == trainerId.Value)
                .Select(tc => tc.ClassId)
                .ToListAsync(ct);

            studentsQuery = studentsQuery.Where(s =>
                (s.BatchId.HasValue && trainerBatchIds.Contains(s.BatchId.Value)) ||
                (s.ClassId.HasValue && trainerClassIds.Contains(s.ClassId.Value)));
        }

        var students = await studentsQuery.OrderBy(s => s.FullName).ToListAsync(ct);
        var allStudentIds = students.Select(s => s.StudentId).ToHashSet();

        var allResults = await _context.Results.AsNoTracking()
            .Where(r => allStudentIds.Contains(r.StudentId))
            .ToListAsync(ct);

        if (dateFrom.HasValue)
            allResults = allResults.Where(r => r.GeneratedAt >= dateFrom.Value).ToList();
        if (dateTo.HasValue)
            allResults = allResults.Where(r => r.GeneratedAt <= dateTo.Value).ToList();
        if (assessmentId.HasValue)
            allResults = allResults.Where(r => r.AssessmentId == assessmentId.Value).ToList();

        var assessmentIds = allResults.Select(r => r.AssessmentId).Distinct().ToHashSet();
        var assessments = await _context.Assessments.AsNoTracking()
            .Where(a => assessmentIds.Contains(a.AssessmentId))
            .Include(a => a.Subject).Include(a => a.Topic)
            .ToListAsync(ct);

        var trainerBatches = await _context.TrainerBatches.AsNoTracking()
            .Include(tb => tb.Trainer)
            .ToListAsync(ct);

        var allPercentages = allResults.GroupBy(r => r.StudentId)
            .Select(g => new { StudentId = g.Key, Avg = g.Average(r => r.Percentage) })
            .OrderByDescending(x => x.Avg).ToList();

        var result = new List<StudentPerformanceRowResponse>();
        foreach (var student in students)
        {
            var studentResults = allResults.Where(r => r.StudentId == student.StudentId).ToList();
            if (studentResults.Count == 0 && performanceLevel != null) continue;

            var totalObtained = studentResults.Sum(r => r.ObtainedMarks);
            var totalMarks = studentResults.Sum(r => r.TotalMarks);
            var pct = totalMarks > 0 ? 100m * totalObtained / totalMarks : 0m;

            if (performanceLevel != null)
            {
                var pass = performanceLevel.Equals("High", StringComparison.OrdinalIgnoreCase) && pct < 75;
                var med = performanceLevel.Equals("Medium", StringComparison.OrdinalIgnoreCase) && (pct < 50 || pct >= 75);
                var low = performanceLevel.Equals("Low", StringComparison.OrdinalIgnoreCase) && pct >= 50;
                if (pass || med || low) continue;
            }

            var overallRank = allPercentages.FindIndex(x => x.StudentId == student.StudentId) + 1;

            var collegeStudentIds = students.Where(s => s.CollegeId == student.CollegeId).Select(s => s.StudentId).ToHashSet();
            var collegeRanked = allPercentages.Where(x => collegeStudentIds.Contains(x.StudentId)).ToList();
            var collegeRank = collegeRanked.FindIndex(x => x.StudentId == student.StudentId) + 1;

            var batchStudentIds = students.Where(s => s.BatchId == student.BatchId).Select(s => s.StudentId).ToHashSet();
            var batchRanked = allPercentages.Where(x => batchStudentIds.Contains(x.StudentId)).ToList();
            var batchRank = batchRanked.FindIndex(x => x.StudentId == student.StudentId) + 1;

            var topicScores = new Dictionary<string, List<decimal>>();
            foreach (var r in studentResults)
            {
                var assessment = assessments.FirstOrDefault(a => a.AssessmentId == r.AssessmentId);
                var topicName = assessment?.Topic?.TopicName ?? assessment?.Subject?.SubjectName ?? "General";
                if (!topicScores.ContainsKey(topicName))
                    topicScores[topicName] = new List<decimal>();
                topicScores[topicName].Add(r.Percentage);
            }
            var rankedTopics = topicScores
                .Select(kv => new { Topic = kv.Key, Avg = kv.Value.Average() })
                .OrderByDescending(x => x.Avg).ToList();

            var trainerName = "N/A";
            if (student.BatchId.HasValue)
            {
                var tb = trainerBatches.FirstOrDefault(t => t.BatchId == student.BatchId.Value);
                if (tb?.Trainer != null) trainerName = tb.Trainer.FullName;
            }

            var trend = "Stable";
            if (studentResults.Count >= 4)
            {
                var ordered = studentResults.OrderBy(r => r.GeneratedAt).ToList();
                var firstHalf = ordered.Take(ordered.Count / 2).Average(r => r.Percentage);
                var secondHalf = ordered.Skip(ordered.Count / 2).Average(r => r.Percentage);
                trend = secondHalf > firstHalf + 5 ? "Improving" : secondHalf < firstHalf - 5 ? "Declining" : "Stable";
            }

            var totalAssessmentsForCollege = await _context.Assessments.AsNoTracking()
                .Where(a => a.CollegeId == student.CollegeId).CountAsync(ct);
            var completionRate = totalAssessmentsForCollege > 0
                ? 100m * studentResults.Select(r => r.AssessmentId).Distinct().Count() / totalAssessmentsForCollege
                : 0m;

            var weakTopics = rankedTopics.TakeLast(3).Select(x => x.Topic).ToList();
            var strongTopics = rankedTopics.Take(3).Select(x => x.Topic).ToList();

            result.Add(new StudentPerformanceRowResponse
            {
                StudentId = student.StudentId.ToString(),
                Name = student.FullName,
                EnrollmentNumber = student.EnrollmentNumber ?? "",
                CollegeName = student.College?.CollegeName ?? "",
                BranchName = student.Class?.Name ?? "",
                Semester = student.Class?.AcademicYear ?? "",
                BatchName = student.Batch?.Name ?? "",
                TrainerName = trainerName,
                Assessments = studentResults.Select(r =>
                {
                    var a = assessments.FirstOrDefault(x => x.AssessmentId == r.AssessmentId);
                    return new AssessmentBreakdownResponse
                    {
                        AssessmentName = a?.AssessmentName ?? "Unknown",
                        AssessmentType = a?.AssessmentType.ToString() ?? "",
                        ObtainedMarks = r.ObtainedMarks,
                        TotalMarks = r.TotalMarks,
                        Percentage = r.Percentage,
                        Rank = r.Rank,
                        Status = r.ResultStatus.ToString(),
                        Date = r.GeneratedAt.ToString("yyyy-MM-dd")
                    };
                }).ToList(),
                TotalMarks = totalMarks,
                TotalObtained = totalObtained,
                OverallPercentage = Math.Round(pct, 2),
                OverallRank = overallRank,
                CollegeRank = collegeRank,
                BatchRank = batchRank,
                CompletionRate = Math.Round(completionRate, 2),
                PerformanceTrend = trend,
                PlacementReadiness = pct >= 80 ? "Excellent" : pct >= 70 ? "Good" : pct >= 50 ? "Average" : "Needs Improvement",
                AiInsights = new StudentAiInsightsResponse
                {
                    LearningGaps = weakTopics.Take(5).Select(t => $"Needs improvement in {t}").ToList(),
                    RootCauseAnalysis = weakTopics.Take(3).Select(t => $"Foundational gaps detected in {t}").ToList(),
                    WeakTopics = weakTopics,
                    StrongTopics = strongTopics,
                    CommunicationGaps = pct < 60 ? new List<string> { "Verbal skills need structured practice", "Written communication needs attention" } : new List<string>(),
                    InterviewReadiness = pct >= 75 ? "Ready" : pct >= 60 ? "Partially Ready" : "Not Ready",
                    RecommendedPracticeAreas = weakTopics.Take(3).Select(t => $"Daily practice sessions on {t}").ToList(),
                    SuggestedResources = weakTopics.Take(3).Select(t => $"Study materials for {t}").ToList(),
                    PriorityLevel = pct < 40 ? "Critical" : pct < 60 ? "High" : pct < 75 ? "Medium" : "Low",
                    ImprovementPlan = BuildImprovementPlan(pct, weakTopics, strongTopics)
                }
            });
        }
        return result;
    }

    private static string GetGrade(decimal percentage) =>
        percentage >= 90 ? "A+" :
        percentage >= 80 ? "A" :
        percentage >= 70 ? "B+" :
        percentage >= 60 ? "B" :
        percentage >= 50 ? "C" :
        percentage >= 40 ? "D" : "F";

    private static List<string> BuildImprovementPlan(decimal percentage, List<string> weakTopics, List<string> strongTopics)
    {
        var plan = new List<string>();
        if (percentage < 40)
        {
            plan.Add("CRITICAL: Immediate intervention required");
            plan.Add("Schedule daily one-on-one mentoring sessions");
            plan.AddRange(weakTopics.Take(3).Select(t => $"Focus on foundational concepts in {t}"));
        }
        else if (percentage < 60)
        {
            plan.Add("Schedule additional practice sessions 3x per week");
            plan.AddRange(weakTopics.Take(3).Select(t => $"Dedicate extra study time to {t}"));
        }
        else if (percentage < 75)
        {
            plan.Add("Focus on advanced problem solving");
            plan.AddRange(weakTopics.Take(2).Select(t => $"Practice advanced problems in {t}"));
            plan.AddRange(strongTopics.Take(2).Select(t => $"Leverage strength in {t} for peer tutoring"));
        }
        else
        {
            plan.Add("Maintain current performance trajectory");
            plan.Add("Focus on interview preparation and soft skills");
            plan.AddRange(strongTopics.Take(2).Select(t => $"Consider mentoring peers in {t}"));
        }
        return plan;
    }
}
