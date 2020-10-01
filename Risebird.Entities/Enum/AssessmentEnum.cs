using System.ComponentModel;

namespace Risebird.Entities.Enum
{
    public enum AssessmentEnum
    {
        [Description("Approved")]
        Approved = 1,
        [Description("Send For Approval")]
        SendForApproval = 0
    }
    public enum Senioritylevel
    {
        [Description("Junior")]
        Junior = 1,
        [Description("Mid Level")]
        MidLevel = 2
    }
    public enum AssessmentType
    {
        [Description("Knowledge Check")]
        KnowledgeCheck = 1,
        [Description("Capability Check")]
        CapabilityCheck = 2,
        [Description("Logic Check")]
        LogicCheck = 3
    }
    public enum Evaluation
    {
        [Description("Automatic")]
        Automatic = 1,
        [Description("Manual")]
        Manual = 2
    }
    public enum EmployerStage
    {
        [Description("Enterprise")]
        Enterprise = 1,
        [Description("Public Limited")]
        PublicLimited = 2
    }
    public enum EmployerSize
    {
        [Description("1000-5000")]
        Level1 = 1,
        [Description("5001-10,000")]
        Level2 = 2,
        [Description("10,001+")]
        Level3 = 3
    }
    public enum AssessmentYearsOfExperience
    {
        [Description("1-3")]
        OneToThree = 1,
        [Description("3-7")]
        ThreeToSeven = 2,
        [Description("7-10")]
        SevenToTez = 3,
        [Description("10+")]
        TenPlus = 4,
    }
    public enum AssessmentLevel
    {
        [Description("Very Easy")]
        VeryEasy = 1,
        [Description("Easy")]
        Easy = 2,
        [Description("Medium")]
        Medium = 3,
        [Description("EaMediumsy")]
        EaMediumsy = 4,
        [Description("Hard")]
        Hard = 5,
    }
}
