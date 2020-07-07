using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Risebird.Entities.Enum
{
    public enum CompanyStage
    {
        [Description("Seed")]
        Seed,
        [Description("Startup")]
        Startup,
        [Description("VC Funded Startup")]
        VCFundedStartup,
        [Description("PE Funded Startup")]
        PEFundedStartup,
        [Description("SME")]
        SME,
        [Description("Enterprise")]
        Enterprise,
        [Description("Public Limited")]
        PublicLimited
    }

    public enum SeniorityLevel 
    {
        [Description("Entry")]
        Entry,
        [Description("Senior")]
        Senior,
        [Description("Lead")]
        Lead,
        [Description("Architect/Manager")]
        ArchitectOrManager,
        [Description("Director")]
        Director,
        [Description("VP")]
        VP,
        [Description("CXO")]
        CXO,
        [Description("Partner")]
        Partner
    }

    public enum WorkNature
    {
        [Description("People Management")]
        PeopleManagement,
        [Description("Individual Contribution")]
        IndividualContribution,
        [Description("Both")]
        Both
    }

    public enum PrimaryResponsibility
    {
        [Description("Technical")]
        Technical,
        [Description("People")]
        People,
        [Description("Administrative")]
        Administrative
    }

    public enum CompanySize
    {
        [Description("Self-employed")]
        Level1 = 1,
        [Description("1-10 employees")]
        Level2 = 2,
        [Description("11-50 employees")]
        Level3 = 3,
        [Description("51-200 employees")]
        Level4 = 4,
        [Description("201-500 employees")]
        Level5 = 5,
        [Description("501-1000 employees")]
        Level6 = 6,
        [Description("1001-5000 employees")]
        Level7 = 7,
        [Description("5001-10,000 employees")]
        Level8 = 8,
        [Description("10,001+ employees")]
        Level9 = 9
    }

    public enum Role
    {
        [Description("Senior Engineering manager")]
        SeniorEngineeringManager,
        [Description("Principal Architect")]
        PrincipalArchitect,
        [Description("Technical Architect")]
        TechnicalArchitect,
        [Description("Solution Architect")]
        SolutionArchitect,
        [Description("Technical Leader")]
        TechnicalLeader,
        [Description("Technology Specialist")]
        TechnologySpecialist,
        [Description("Technical Lead")]
        TechnicalLead,
        [Description("Enterprise Architect")]
        EnterpriseArchitect,
        [Description("Technical Project Manager")]
        TechnicalProjectManager,
        [Description("Technical Expert")]
        TechnicalExpert,
        [Description("Engineering lead")]
        EngineeringLead,
        [Description("Lead Project Engineer")]
        LeadProjectEngineer,
        [Description("Technical project lead")]
        TechnicalProjectLead,
        [Description("Senior Engineer")]
        SeniorEngineer,


    }
}
