using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Taskverse.Data.DataAccess;

[Table("coding_questions")]
public class CodingQuestion
{
    [Key]
    [Column("coding_question_id")]
    public Guid CodingQuestionId { get; set; } = Guid.NewGuid();

    [Required]
    [Column("college_id")]
    public Guid CollegeId { get; set; }

    [Required]
    [MaxLength(250)]
    [Column("question_title")]
    public string QuestionTitle { get; set; } = default!;

    [Required]
    [Column("problem_statement")]
    public string ProblemStatement { get; set; } = default!;

    [Column("detailed_description")]
    public string? DetailedDescription { get; set; }

    [Column("difficulty_level")]
    public int DifficultyLevel { get; set; } = 1;

    [Required]
    [MaxLength(50)]
    [Column("question_type")]
    public string QuestionType { get; set; } = "coding";

    [Column("topic_tag", TypeName = "text[]")]
    public string[]? TopicTag { get; set; }

    [Column("input_format")]
    public string? InputFormat { get; set; }

    [Column("output_format")]
    public string? OutputFormat { get; set; }

    [Column("constraints_text")]
    public string? ConstraintsText { get; set; }

    [Column("explanation")]
    public string? Explanation { get; set; }

    [Column("examples", TypeName = "jsonb")]
    public string? Examples { get; set; }

    [MaxLength(50)]
    [Column("default_language_code")]
    public string? DefaultLanguageCode { get; set; }

    [Column("default_time_limit_ms")]
    public int DefaultTimeLimitMs { get; set; } = 3000;

    [Column("default_memory_limit_kb")]
    public int DefaultMemoryLimitKb { get; set; } = 262144;

    [Column("default_max_code_size_kb")]
    public int DefaultMaxCodeSizeKb { get; set; } = 512;

    [Column("marks", TypeName = "numeric(8,2)")]
    public decimal Marks { get; set; } = 100m;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_by")]
    public Guid? CreatedBy { get; set; }

    [Column("modified_by")]
    public Guid? ModifiedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("modified_at")]
    public DateTime? ModifiedAt { get; set; }

    [Column("version")]
    public int Version { get; set; } = 1;

    public College College { get; set; } = default!;

    public ICollection<TestCase> TestCases { get; set; } = [];

    public ICollection<AssessmentCodingQuestion> AssessmentCodingQuestions { get; set; } = [];
}
