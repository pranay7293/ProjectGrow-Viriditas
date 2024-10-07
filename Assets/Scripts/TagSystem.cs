using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public static class TagSystem
{
    // Action Tags
    public static class ActionTags
    {
        public const string Treat = "Treat";
        public const string Research = "Research";
        public const string Consult = "Consult";
        public const string Debate = "Debate";
        public const string Strategize = "Strategize";
        public const string Philosophize = "Philosophize";
        public const string Broadcast = "Broadcast";
        public const string Interview = "Interview";
        public const string Produce = "Produce";
        public const string Ideate = "Ideate";
        public const string Prototype = "Prototype";
        public const string Pitch = "Pitch";
        public const string Tinker = "Tinker";
        public const string Fabricate = "Fabricate";
        public const string Collaborate = "Collaborate";
        public const string Experiment = "Experiment";
        public const string Analyze = "Analyze";
        public const string Document = "Document";
        public const string Model = "Model";
        public const string Forecast = "Forecast";
        public const string Trade = "Trade";
        public const string Create = "Create";
        public const string Exhibit = "Exhibit";
        public const string Critique = "Critique";
        public const string Engineer = "Engineer";
        public const string Cultivate = "Cultivate";
        public const string Automate = "Automate";
        public const string Simulate = "Simulate";
        public const string Innovate = "Innovate";
        public const string Explore = "Explore";
    }

    // Challenge Tags
    public static class ChallengeTags
    {
        // Mammoth Revival
        public const string ExtractDNA = "ExtractDNA";
        public const string ReconstructGenome = "ReconstructGenome";
        public const string CreateEmbryos = "CreateEmbryos";
        public const string EstablishEthics = "EstablishEthics";
        public const string PrepareHabitat = "PrepareHabitat";

        // Space Farm
        public const string SelectCrops = "SelectCrops";
        public const string OptimizeCrops = "OptimizeCrops";
        public const string DesignHydroponics = "DesignHydroponics";
        public const string TestGrowthConditions = "TestGrowthConditions";
        public const string DeploySpaceFood = "DeploySpaceFood";

        // Mind-Machine Link
        public const string MapBrainActivity = "MapBrainActivity";
        public const string CreateBCI = "CreateBCI";
        public const string ConductTrials = "ConductTrials";
        public const string AddressEthics = "AddressEthics";
        public const string PlanProduction = "PlanProduction";

        // Fast-Fashion 2.0
        public const string DevelopMaterials = "DevelopMaterials";
        public const string DesignFashion = "DesignFashion";
        public const string ScaleProduction = "ScaleProduction";
        public const string CreatePackaging = "CreatePackaging";
        public const string LaunchMarketing = "LaunchMarketing";

        // Cultured Cuisine
        public const string OptimizeMeatTechniques = "OptimizeMeatTechniques";
        public const string DesignBioreactors = "DesignBioreactors";
        public const string ReduceCosts = "ReduceCosts";
        public const string EnsureCompliance = "EnsureCompliance";
        public const string PlanMarketEntry = "PlanMarketEntry";

        // Biodefense Shield
        public const string IdentifyThreats = "IdentifyThreats";
        public const string CreateDetection = "CreateDetection";
        public const string EstablishProtocols = "EstablishProtocols";
        public const string IntegrateDefenses = "IntegrateDefenses";
        public const string FosterCooperation = "FosterCooperation";

        // Cross-Challenge Tags
        public const string InnovateProcess = "InnovateProcess";
        public const string SecureFunding = "SecureFunding";
        public const string PublicOutreach = "PublicOutreach";
        public const string CollaborateAcrossFields = "CollaborateAcrossFields";
        public const string EthicalConsiderations = "EthicalConsiderations";
    }

    // Personal Goal Tags
    public static class PersonalGoalTags
    {
        public const string EfficientSolution = "EfficientSolution";
        public const string AutomateProcesses = "AutomateProcesses";
        public const string BalanceInnovation = "BalanceInnovation";
        public const string AdaptEarthSolutions = "AdaptEarthSolutions";
        public const string ProveRelevance = "ProveRelevance";
        public const string BalanceMission = "BalanceMission";
        public const string TranslateScience = "TranslateScience";
        public const string SecureResources = "SecureResources";
        public const string ProveCreativity = "ProveCreativity";
        public const string PublishResearch = "PublishResearch";
        public const string SecureFundingPersonal = "SecureFundingPersonal"; // Renamed to avoid conflict with ChallengeTags
        public const string MaintainIntegrity = "MaintainIntegrity";
        public const string PrioritizeWellbeing = "PrioritizeWellbeing";
        public const string FastTrackApplications = "FastTrackApplications";
        public const string NavigatePrivacy = "NavigatePrivacy";
        public const string ResolveConflicts = "ResolveConflicts";
        public const string BalanceEthics = "BalanceEthics";
        public const string ConductDIY = "ConductDIY";
        public const string ShareTechniques = "ShareTechniques";
        public const string DevelopOpenSource = "DevelopOpenSource";
        public const string UncoverChallenges = "UncoverChallenges";
        public const string GainAccess = "GainAccess";
        public const string BalanceTransparency = "BalanceTransparency";
        public const string DevelopProfitableModel = "DevelopProfitableModel";
        public const string AdvocateCostCutting = "AdvocateCostCutting";
        public const string BalanceSustainability = "BalanceSustainability";
        public const string CommercializeInnovation = "CommercializeInnovation";
        public const string BuildTeam = "BuildTeam";
        public const string BalanceProfitEthics = "BalanceProfitEthics";
    }

    // Mapping of Actions to Tags with Weights
    public static Dictionary<string, List<(string tag, float weight)>> ActionToTags = new Dictionary<string, List<(string tag, float weight)>>()
    {
        { ActionTags.Treat, new List<(string, float)>
            {
                (ChallengeTags.ConductTrials, 0.05f),
                (ChallengeTags.EnsureCompliance, 0.05f),
                (PersonalGoalTags.PrioritizeWellbeing, 0.1f)
            }
        },
        { ActionTags.Research, new List<(string, float)>
            {
                (ChallengeTags.ExtractDNA, 0.05f),
                (ChallengeTags.ReconstructGenome, 0.05f),
                (ChallengeTags.IdentifyThreats, 0.05f),
                (ChallengeTags.InnovateProcess, 0.05f),
                (PersonalGoalTags.PublishResearch, 0.1f)
            }
        },
        { ActionTags.Consult, new List<(string, float)>
            {
                (ChallengeTags.FosterCooperation, 0.05f),
                (ChallengeTags.CollaborateAcrossFields, 0.05f),
                (PersonalGoalTags.ResolveConflicts, 0.1f)
            }
        },
        { ActionTags.Debate, new List<(string, float)>
            {
                (ChallengeTags.EthicalConsiderations, 0.05f),
                (PersonalGoalTags.BalanceEthics, 0.1f),
                (PersonalGoalTags.ResolveConflicts, 0.05f)
            }
        },
        { ActionTags.Strategize, new List<(string, float)>
            {
                (ChallengeTags.PlanProduction, 0.05f),
                (ChallengeTags.EstablishProtocols, 0.05f),
                (PersonalGoalTags.EfficientSolution, 0.1f)
            }
        },
        { ActionTags.Philosophize, new List<(string, float)>
            {
                (ChallengeTags.EstablishEthics, 0.05f),
                (ChallengeTags.EthicalConsiderations, 0.05f),
                (PersonalGoalTags.MaintainIntegrity, 0.1f)
            }
        },
        { ActionTags.Broadcast, new List<(string, float)>
            {
                (ChallengeTags.PublicOutreach, 0.1f),
                (PersonalGoalTags.TranslateScience, 0.08f),
                (PersonalGoalTags.ProveRelevance, 0.05f)
            }
        },
        { ActionTags.Interview, new List<(string, float)>
            {
                (ChallengeTags.PublicOutreach, 0.05f),
                (PersonalGoalTags.TranslateScience, 0.1f),
                (PersonalGoalTags.GainAccess, 0.05f)
            }
        },
        { ActionTags.Produce, new List<(string, float)>
            {
                (ChallengeTags.ScaleProduction, 0.05f),
                (ChallengeTags.PlanProduction, 0.05f),
                (PersonalGoalTags.CommercializeInnovation, 0.1f)
            }
        },
        { ActionTags.Ideate, new List<(string, float)>
            {
                (ChallengeTags.InnovateProcess, 0.05f),
                (PersonalGoalTags.ProveCreativity, 0.1f),
                (PersonalGoalTags.EfficientSolution, 0.05f)
            }
        },
        { ActionTags.Prototype, new List<(string, float)>
            {
                (ChallengeTags.InnovateProcess, 0.05f),
                (PersonalGoalTags.AdaptEarthSolutions, 0.1f),
                (PersonalGoalTags.FastTrackApplications, 0.05f)
            }
        },
        { ActionTags.Pitch, new List<(string, float)>
            {
                (ChallengeTags.SecureFunding, 0.05f),
                (PersonalGoalTags.SecureFundingPersonal, 0.1f),
                (PersonalGoalTags.DevelopProfitableModel, 0.05f)
            }
        },
        { ActionTags.Tinker, new List<(string, float)>
            {
                (ChallengeTags.TestGrowthConditions, 0.05f),
                (PersonalGoalTags.ConductDIY, 0.1f),
                (PersonalGoalTags.ShareTechniques, 0.05f)
            }
        },
        { ActionTags.Fabricate, new List<(string, float)>
            {
                (ChallengeTags.DevelopMaterials, 0.05f),
                (ChallengeTags.DesignBioreactors, 0.05f),
                (PersonalGoalTags.AutomateProcesses, 0.1f)
            }
        },
        { ActionTags.Collaborate, new List<(string, float)>
            {
                (ChallengeTags.FosterCooperation, 0.07f),
                (ChallengeTags.CollaborateAcrossFields, 0.05f),
                (PersonalGoalTags.BuildTeam, 0.05f)
            }
        },
        { ActionTags.Experiment, new List<(string, float)>
            {
                (ChallengeTags.ConductTrials, 0.05f),
                (ChallengeTags.TestGrowthConditions, 0.05f),
                (PersonalGoalTags.PublishResearch, 0.1f)
            }
        },
        { ActionTags.Analyze, new List<(string, float)>
            {
                (ChallengeTags.IdentifyThreats, 0.05f),
                (PersonalGoalTags.UncoverChallenges, 0.1f),
                (PersonalGoalTags.EfficientSolution, 0.05f)
            }
        },
        { ActionTags.Document, new List<(string, float)>
            {
                (ChallengeTags.EstablishProtocols, 0.05f),
                (PersonalGoalTags.PublishResearch, 0.1f),
                (PersonalGoalTags.MaintainIntegrity, 0.05f)
            }
        },
        { ActionTags.Model, new List<(string, float)>
            {
                (ChallengeTags.InnovateProcess, 0.05f),
                (PersonalGoalTags.EfficientSolution, 0.1f),
                (PersonalGoalTags.BalanceInnovation, 0.05f)
            }
        },
        { ActionTags.Forecast, new List<(string, float)>
            {
                (ChallengeTags.PlanProduction, 0.05f),
                (PersonalGoalTags.BalanceSustainability, 0.1f),
                (PersonalGoalTags.BalanceMission, 0.05f)
            }
        },
        { ActionTags.Trade, new List<(string, float)>
            {
                (ChallengeTags.PlanMarketEntry, 0.05f),
                (PersonalGoalTags.DevelopProfitableModel, 0.1f),
                (PersonalGoalTags.AdvocateCostCutting, 0.05f)
            }
        },
        { ActionTags.Create, new List<(string, float)>
            {
                (ChallengeTags.CreateBCI, 0.05f),
                (ChallengeTags.CreatePackaging, 0.05f),
                (PersonalGoalTags.ProveCreativity, 0.1f)
            }
        },
        { ActionTags.Exhibit, new List<(string, float)>
            {
                (ChallengeTags.PublicOutreach, 0.05f),
                (PersonalGoalTags.TranslateScience, 0.1f),
                (PersonalGoalTags.ProveRelevance, 0.05f)
            }
        },
        { ActionTags.Critique, new List<(string, float)>
            {
                (ChallengeTags.EthicalConsiderations, 0.05f),
                (PersonalGoalTags.MaintainIntegrity, 0.1f),
                (PersonalGoalTags.BalanceEthics, 0.05f)
            }
        },
        { ActionTags.Engineer, new List<(string, float)>
            {
                (ChallengeTags.DesignHydroponics, 0.05f),
                (ChallengeTags.DesignBioreactors, 0.05f),
                (PersonalGoalTags.AutomateProcesses, 0.1f)
            }
        },
        { ActionTags.Cultivate, new List<(string, float)>
            {
                (ChallengeTags.SelectCrops, 0.05f),
                (ChallengeTags.OptimizeCrops, 0.05f),
                (PersonalGoalTags.BalanceSustainability, 0.1f)
            }
        },
        { ActionTags.Automate, new List<(string, float)>
            {
                (ChallengeTags.InnovateProcess, 0.05f),
                (PersonalGoalTags.AutomateProcesses, 0.1f),
                (PersonalGoalTags.EfficientSolution, 0.05f)
            }
        },
        { ActionTags.Simulate, new List<(string, float)>
            {
                (ChallengeTags.MapBrainActivity, 0.05f),
                (PersonalGoalTags.EfficientSolution, 0.1f),
                (PersonalGoalTags.BalanceInnovation, 0.05f)
            }
        },
        { ActionTags.Innovate, new List<(string, float)>
            {
                (ChallengeTags.InnovateProcess, 0.1f),
                (PersonalGoalTags.EfficientSolution, 0.08f),
                (PersonalGoalTags.CommercializeInnovation, 0.05f)
            }
        },
        { ActionTags.Explore, new List<(string, float)>
            {
                (ChallengeTags.PrepareHabitat, 0.05f),
                (PersonalGoalTags.UncoverChallenges, 0.1f),
                (PersonalGoalTags.ProveRelevance, 0.05f)
            }
        }
    };

    // Lists for Validation
    public static List<string> PersonalGoalTagsList = new List<string>()
    {
        PersonalGoalTags.EfficientSolution,
        PersonalGoalTags.AutomateProcesses,
        PersonalGoalTags.BalanceInnovation,
        PersonalGoalTags.AdaptEarthSolutions,
        PersonalGoalTags.ProveRelevance,
        PersonalGoalTags.BalanceMission,
        PersonalGoalTags.TranslateScience,
        PersonalGoalTags.SecureResources,
        PersonalGoalTags.ProveCreativity,
        PersonalGoalTags.PublishResearch,
        PersonalGoalTags.SecureFundingPersonal,
        PersonalGoalTags.MaintainIntegrity,
        PersonalGoalTags.PrioritizeWellbeing,
        PersonalGoalTags.FastTrackApplications,
        PersonalGoalTags.NavigatePrivacy,
        PersonalGoalTags.ResolveConflicts,
        PersonalGoalTags.BalanceEthics,
        PersonalGoalTags.ConductDIY,
        PersonalGoalTags.ShareTechniques,
        PersonalGoalTags.DevelopOpenSource,
        PersonalGoalTags.UncoverChallenges,
        PersonalGoalTags.GainAccess,
        PersonalGoalTags.BalanceTransparency,
        PersonalGoalTags.DevelopProfitableModel,
        PersonalGoalTags.AdvocateCostCutting,
        PersonalGoalTags.BalanceSustainability,
        PersonalGoalTags.CommercializeInnovation,
        PersonalGoalTags.BuildTeam,
        PersonalGoalTags.BalanceProfitEthics
    };

    public static List<string> ChallengeTagsList = new List<string>()
    {
        ChallengeTags.ExtractDNA,
        ChallengeTags.ReconstructGenome,
        ChallengeTags.CreateEmbryos,
        ChallengeTags.EstablishEthics,
        ChallengeTags.PrepareHabitat,
        ChallengeTags.SelectCrops,
        ChallengeTags.OptimizeCrops,
        ChallengeTags.DesignHydroponics,
        ChallengeTags.TestGrowthConditions,
        ChallengeTags.DeploySpaceFood,
        ChallengeTags.MapBrainActivity,
        ChallengeTags.CreateBCI,
        ChallengeTags.ConductTrials,
        ChallengeTags.AddressEthics,
        ChallengeTags.PlanProduction,
        ChallengeTags.DevelopMaterials,
        ChallengeTags.DesignFashion,
        ChallengeTags.ScaleProduction,
        ChallengeTags.CreatePackaging,
        ChallengeTags.LaunchMarketing,
        ChallengeTags.OptimizeMeatTechniques,
        ChallengeTags.DesignBioreactors,
        ChallengeTags.ReduceCosts,
        ChallengeTags.EnsureCompliance,
        ChallengeTags.PlanMarketEntry,
        ChallengeTags.IdentifyThreats,
        ChallengeTags.CreateDetection,
        ChallengeTags.EstablishProtocols,
        ChallengeTags.IntegrateDefenses,
        ChallengeTags.FosterCooperation,
        ChallengeTags.InnovateProcess,
        ChallengeTags.SecureFunding,
        ChallengeTags.PublicOutreach,
        ChallengeTags.CollaborateAcrossFields,
        ChallengeTags.EthicalConsiderations
    };

    public static List<string> ActionTagsList = new List<string>()
    {
        ActionTags.Treat,
        ActionTags.Research,
        ActionTags.Consult,
        ActionTags.Debate,
        ActionTags.Strategize,
        ActionTags.Philosophize,
        ActionTags.Broadcast,
        ActionTags.Interview,
        ActionTags.Produce,
        ActionTags.Ideate,
        ActionTags.Prototype,
        ActionTags.Pitch,
        ActionTags.Tinker,
        ActionTags.Fabricate,
        ActionTags.Collaborate,
        ActionTags.Experiment,
        ActionTags.Analyze,
        ActionTags.Document,
        ActionTags.Model,
        ActionTags.Forecast,
        ActionTags.Trade,
        ActionTags.Create,
        ActionTags.Exhibit,
        ActionTags.Critique,
        ActionTags.Engineer,
        ActionTags.Cultivate,
        ActionTags.Automate,
        ActionTags.Simulate,
        ActionTags.Innovate,
        ActionTags.Explore
    };

    // Method to get tags for an action
    public static List<(string tag, float weight)> GetTagsForAction(string actionName)
    {
        if (ActionToTags.TryGetValue(actionName, out List<(string tag, float weight)> tags))
        {
            return tags;
        }
        return new List<(string, float)>();
    }

    // Method to Validate and Filter Eureka Tags
    public static List<string> ValidateEurekaTags(List<string> generatedTags)
    {
        List<string> validTags = new List<string>();
        foreach (var tag in generatedTags)
        {
            if (PersonalGoalTagsList.Contains(tag) || ChallengeTagsList.Contains(tag))
            {
                validTags.Add(tag);
            }
            else
            {
                Debug.LogWarning($"Invalid tag generated by Eureka: {tag}");
            }
        }
        return validTags;
    }
}

// using UnityEngine;
// using System.Collections.Generic;

// public static class TagSystem
// {
//     // Action Tags
//     public static class ActionTags
//     {
//         public const string Treat = "Treat";
//         public const string Research = "Research";
//         public const string Consult = "Consult";
//         public const string Debate = "Debate";
//         public const string Strategize = "Strategize";
//         public const string Philosophize = "Philosophize";
//         public const string Broadcast = "Broadcast";
//         public const string Interview = "Interview";
//         public const string Produce = "Produce";
//         public const string Ideate = "Ideate";
//         public const string Prototype = "Prototype";
//         public const string Pitch = "Pitch";
//         public const string Tinker = "Tinker";
//         public const string Fabricate = "Fabricate";
//         public const string Collaborate = "Collaborate";
//         public const string Experiment = "Experiment";
//         public const string Analyze = "Analyze";
//         public const string Document = "Document";
//         public const string Model = "Model";
//         public const string Forecast = "Forecast";
//         public const string Trade = "Trade";
//         public const string Create = "Create";
//         public const string Exhibit = "Exhibit";
//         public const string Critique = "Critique";
//         public const string Engineer = "Engineer";
//         public const string Cultivate = "Cultivate";
//         public const string Automate = "Automate";
//         public const string Simulate = "Simulate";
//         public const string Innovate = "Innovate";
//         public const string Explore = "Explore";
//     }

//     // Challenge Tags
//     public static class ChallengeTags
//     {
//          // Mammoth Revival
//         public const string ExtractDNA = "ExtractDNA";
//         public const string ReconstructGenome = "ReconstructGenome";
//         public const string CreateEmbryos = "CreateEmbryos";
//         public const string EstablishEthics = "EstablishEthics";
//         public const string PrepareHabitat = "PrepareHabitat";

//         // Space Farm
//         public const string SelectCrops = "SelectCrops";
//         public const string OptimizeCrops = "OptimizeCrops";
//         public const string DesignHydroponics = "DesignHydroponics";
//         public const string TestGrowthConditions = "TestGrowthConditions";
//         public const string DeploySpaceFood = "DeploySpaceFood";

//         // Mind-Machine Link
//         public const string MapBrainActivity = "MapBrainActivity";
//         public const string CreateBCI = "CreateBCI";
//         public const string ConductTrials = "ConductTrials";
//         public const string AddressEthics = "AddressEthics";
//         public const string PlanProduction = "PlanProduction";

//         // Fast-Fashion 2.0
//         public const string DevelopMaterials = "DevelopMaterials";
//         public const string DesignFashion = "DesignFashion";
//         public const string ScaleProduction = "ScaleProduction";
//         public const string CreatePackaging = "CreatePackaging";
//         public const string LaunchMarketing = "LaunchMarketing";

//         // Cultured Cuisine
//         public const string OptimizeMeatTechniques = "OptimizeMeatTechniques";
//         public const string DesignBioreactors = "DesignBioreactors";
//         public const string ReduceCosts = "ReduceCosts";
//         public const string EnsureCompliance = "EnsureCompliance";
//         public const string PlanMarketEntry = "PlanMarketEntry";

//         // Biodefense Shield
//         public const string IdentifyThreats = "IdentifyThreats";
//         public const string CreateDetection = "CreateDetection";
//         public const string EstablishProtocols = "EstablishProtocols";
//         public const string IntegrateDefenses = "IntegrateDefenses";
//         public const string FosterCooperation = "FosterCooperation";

//         // Cross-Challenge Tags
//         public const string InnovateProcess = "InnovateProcess";
//         public const string SecureFunding = "SecureFunding";
//         public const string PublicOutreach = "PublicOutreach";
//         public const string CollaborateAcrossFields = "CollaborateAcrossFields";
//         public const string EthicalConsiderations = "EthicalConsiderations";
//     }

//     // Personal Goal Tags
//     public static class PersonalGoalTags
//     {
//         public const string EfficientSolution = "EfficientSolution";
//         public const string AutomateProcesses = "AutomateProcesses";
//         public const string BalanceInnovation = "BalanceInnovation";
//         public const string AdaptEarthSolutions = "AdaptEarthSolutions";
//         public const string ProveRelevance = "ProveRelevance";
//         public const string BalanceMission = "BalanceMission";
//         public const string TranslateScience = "TranslateScience";
//         public const string SecureResources = "SecureResources";
//         public const string ProveCreativity = "ProveCreativity";
//         public const string PublishResearch = "PublishResearch";
//         public const string SecureFunding = "SecureFunding";
//         public const string MaintainIntegrity = "MaintainIntegrity";
//         public const string PrioritizeWellbeing = "PrioritizeWellbeing";
//         public const string FastTrackApplications = "FastTrackApplications";
//         public const string NavigatePrivacy = "NavigatePrivacy";
//         public const string EstablishEthics = "EstablishEthics";
//         public const string ResolveConflicts = "ResolveConflicts";
//         public const string BalanceEthics = "BalanceEthics";
//         public const string ConductDIY = "ConductDIY";
//         public const string ShareTechniques = "ShareTechniques";
//         public const string DevelopOpenSource = "DevelopOpenSource";
//         public const string UncoverChallenges = "UncoverChallenges";
//         public const string GainAccess = "GainAccess";
//         public const string BalanceTransparency = "BalanceTransparency";
//         public const string DevelopProfitableModel = "DevelopProfitableModel";
//         public const string AdvocateCostCutting = "AdvocateCostCutting";
//         public const string BalanceSustainability = "BalanceSustainability";
//         public const string CommercializeInnovation = "CommercializeInnovation";
//         public const string BuildTeam = "BuildTeam";
//         public const string BalanceProfitEthics = "BalanceProfitEthics";
//     }

//     public static Dictionary<string, List<string>> ActionToTags = new Dictionary<string, List<string>>
//     {
//         { ActionTags.Treat, new List<string> { ChallengeTags.ConductTrials, ChallengeTags.EnsureCompliance, PersonalGoalTags.PrioritizeWellbeing } },
//         { ActionTags.Research, new List<string> { ChallengeTags.ExtractDNA, ChallengeTags.ReconstructGenome, ChallengeTags.IdentifyThreats, ChallengeTags.InnovateProcess, PersonalGoalTags.PublishResearch } },
//         { ActionTags.Consult, new List<string> { ChallengeTags.EstablishEthics, ChallengeTags.EstablishProtocols, ChallengeTags.EthicalConsiderations, PersonalGoalTags.NavigatePrivacy } },
//         { ActionTags.Debate, new List<string> { ChallengeTags.AddressEthics, ChallengeTags.FosterCooperation, ChallengeTags.EthicalConsiderations, PersonalGoalTags.ResolveConflicts } },
//         { ActionTags.Strategize, new List<string> { ChallengeTags.PlanProduction, ChallengeTags.PlanMarketEntry, ChallengeTags.InnovateProcess, PersonalGoalTags.DevelopProfitableModel } },
//         { ActionTags.Philosophize, new List<string> { ChallengeTags.EstablishEthics, ChallengeTags.EthicalConsiderations, PersonalGoalTags.BalanceEthics } },
//         { ActionTags.Broadcast, new List<string> { ChallengeTags.LaunchMarketing, ChallengeTags.PublicOutreach, PersonalGoalTags.ProveRelevance } },
//         { ActionTags.Interview, new List<string> { ChallengeTags.PublicOutreach, ChallengeTags.CollaborateAcrossFields, PersonalGoalTags.GainAccess } },
//         { ActionTags.Produce, new List<string> { ChallengeTags.CreatePackaging, ChallengeTags.DeploySpaceFood, ChallengeTags.ScaleProduction, PersonalGoalTags.CommercializeInnovation } },
//         { ActionTags.Ideate, new List<string> { ChallengeTags.DevelopMaterials, ChallengeTags.CreateDetection, ChallengeTags.InnovateProcess, PersonalGoalTags.EfficientSolution } },
//         { ActionTags.Prototype, new List<string> { ChallengeTags.CreateBCI, ChallengeTags.DesignBioreactors, ChallengeTags.InnovateProcess, PersonalGoalTags.DevelopOpenSource } },
//         { ActionTags.Pitch, new List<string> { ChallengeTags.SecureFunding, ChallengeTags.PublicOutreach, PersonalGoalTags.CommercializeInnovation } },
//         { ActionTags.Tinker, new List<string> { ChallengeTags.OptimizeCrops, ChallengeTags.OptimizeMeatTechniques, ChallengeTags.InnovateProcess, PersonalGoalTags.ConductDIY } },
//         { ActionTags.Fabricate, new List<string> { ChallengeTags.CreateEmbryos, ChallengeTags.DesignHydroponics, ChallengeTags.ScaleProduction, PersonalGoalTags.AutomateProcesses } },
//         { ActionTags.Collaborate, new List<string> { ChallengeTags.FosterCooperation, ChallengeTags.CollaborateAcrossFields, PersonalGoalTags.BuildTeam } },
//         { ActionTags.Experiment, new List<string> { ChallengeTags.TestGrowthConditions, ChallengeTags.ConductTrials, ChallengeTags.InnovateProcess, PersonalGoalTags.PublishResearch } },
//         { ActionTags.Analyze, new List<string> { ChallengeTags.MapBrainActivity, ChallengeTags.IdentifyThreats, ChallengeTags.EnsureCompliance, PersonalGoalTags.MaintainIntegrity } },
//         { ActionTags.Document, new List<string> { ChallengeTags.EstablishProtocols, ChallengeTags.EnsureCompliance, PersonalGoalTags.BalanceTransparency } },
//         { ActionTags.Model, new List<string> { ChallengeTags.ReconstructGenome, ChallengeTags.ReduceCosts, ChallengeTags.InnovateProcess, PersonalGoalTags.DevelopProfitableModel } },
//         { ActionTags.Forecast, new List<string> { ChallengeTags.PlanMarketEntry, ChallengeTags.IdentifyThreats, PersonalGoalTags.BalanceSustainability } },
//         { ActionTags.Trade, new List<string> { ChallengeTags.SecureFunding, ChallengeTags.DeploySpaceFood, PersonalGoalTags.AdvocateCostCutting } },
//         { ActionTags.Create, new List<string> { ChallengeTags.DevelopMaterials, ChallengeTags.CreateDetection, ChallengeTags.InnovateProcess, PersonalGoalTags.ProveCreativity } },
//         { ActionTags.Exhibit, new List<string> { ChallengeTags.LaunchMarketing, ChallengeTags.PublicOutreach, PersonalGoalTags.TranslateScience } },
//         { ActionTags.Critique, new List<string> { ChallengeTags.EnsureCompliance, ChallengeTags.EthicalConsiderations, PersonalGoalTags.MaintainIntegrity } },
//         { ActionTags.Engineer, new List<string> { ChallengeTags.CreateBCI, ChallengeTags.IntegrateDefenses, ChallengeTags.InnovateProcess, PersonalGoalTags.EfficientSolution } },
//         { ActionTags.Cultivate, new List<string> { ChallengeTags.OptimizeCrops, ChallengeTags.PrepareHabitat, ChallengeTags.CollaborateAcrossFields, PersonalGoalTags.BalanceInnovation } },
//         { ActionTags.Automate, new List<string> { ChallengeTags.ScaleProduction, ChallengeTags.InnovateProcess, PersonalGoalTags.AutomateProcesses } },
//         { ActionTags.Simulate, new List<string> { ChallengeTags.TestGrowthConditions, ChallengeTags.MapBrainActivity, ChallengeTags.InnovateProcess, PersonalGoalTags.AdaptEarthSolutions } },
//         { ActionTags.Innovate, new List<string> { ChallengeTags.CreateDetection, ChallengeTags.DevelopMaterials, ChallengeTags.InnovateProcess, PersonalGoalTags.CommercializeInnovation } },
//         { ActionTags.Explore, new List<string> { ChallengeTags.SelectCrops, ChallengeTags.IdentifyThreats, ChallengeTags.CollaborateAcrossFields, PersonalGoalTags.UncoverChallenges } }
//     };

//     public static List<string> GetTagsForAction(string actionName)
//     {
//         if (ActionToTags.TryGetValue(actionName, out List<string> tags))
//         {
//             return tags;
//         }
//         return new List<string>();
//     }
// }