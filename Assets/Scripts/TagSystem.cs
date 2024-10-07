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

    // Milestone Tags
    public static class MilestoneTags
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
        public const string SecureFundingPersonal = "SecureFundingPersonal"; // Renamed to avoid conflict with MilestoneTags
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

    public static int GetSliderIndexForTag(string tag, AISettings aiSettings = null, ChallengeData challengeData = null)
    {
        if (aiSettings != null && aiSettings.tagToSliderIndex.TryGetValue(tag, out int personalIndex))
        {
            return personalIndex;
        }
        if (challengeData != null && challengeData.tagToSliderIndex.TryGetValue(tag, out int challengeIndex))
        {
            return challengeIndex;
        }
        return -1; // Tag not found
    }

    // Mapping of Actions to Tags with Weights
    public static Dictionary<string, List<(string tag, float weight)>> ActionToTags = new Dictionary<string, List<(string tag, float weight)>>()
    {
        { ActionTags.Treat, new List<(string, float)>
            {
                (MilestoneTags.ConductTrials, 0.05f),
                (MilestoneTags.EnsureCompliance, 0.05f),
                (PersonalGoalTags.PrioritizeWellbeing, 0.1f)
            }
        },
        { ActionTags.Research, new List<(string, float)>
            {
                (MilestoneTags.ExtractDNA, 0.05f),
                (MilestoneTags.ReconstructGenome, 0.05f),
                (MilestoneTags.IdentifyThreats, 0.05f),
                (PersonalGoalTags.PublishResearch, 0.1f)
            }
        },
        { ActionTags.Consult, new List<(string, float)>
            {
                (MilestoneTags.FosterCooperation, 0.05f),
                (PersonalGoalTags.ResolveConflicts, 0.1f)
            }
        },
        { ActionTags.Debate, new List<(string, float)>
            {
                (PersonalGoalTags.BalanceEthics, 0.1f),
                (PersonalGoalTags.ResolveConflicts, 0.05f)
            }
        },
        { ActionTags.Strategize, new List<(string, float)>
            {
                (MilestoneTags.PlanProduction, 0.05f),
                (MilestoneTags.EstablishProtocols, 0.05f),
                (PersonalGoalTags.EfficientSolution, 0.1f)
            }
        },
        { ActionTags.Philosophize, new List<(string, float)>
            {
                (MilestoneTags.EstablishEthics, 0.05f),
                (PersonalGoalTags.MaintainIntegrity, 0.1f)
            }
        },
        { ActionTags.Broadcast, new List<(string, float)>
            {
                (PersonalGoalTags.TranslateScience, 0.08f),
                (PersonalGoalTags.ProveRelevance, 0.05f)
            }
        },
        { ActionTags.Interview, new List<(string, float)>
            {
                (PersonalGoalTags.TranslateScience, 0.1f),
                (PersonalGoalTags.GainAccess, 0.05f)
            }
        },
        { ActionTags.Produce, new List<(string, float)>
            {
                (MilestoneTags.ScaleProduction, 0.05f),
                (MilestoneTags.PlanProduction, 0.05f),
                (PersonalGoalTags.CommercializeInnovation, 0.1f)
            }
        },
        { ActionTags.Ideate, new List<(string, float)>
            {
                (PersonalGoalTags.ProveCreativity, 0.1f),
                (PersonalGoalTags.EfficientSolution, 0.05f)
            }
        },
        { ActionTags.Prototype, new List<(string, float)>
            {
                (PersonalGoalTags.AdaptEarthSolutions, 0.1f),
                (PersonalGoalTags.FastTrackApplications, 0.05f)
            }
        },
        { ActionTags.Pitch, new List<(string, float)>
            {
                (PersonalGoalTags.SecureFundingPersonal, 0.1f),
                (PersonalGoalTags.DevelopProfitableModel, 0.05f)
            }
        },
        { ActionTags.Tinker, new List<(string, float)>
            {
                (MilestoneTags.TestGrowthConditions, 0.05f),
                (PersonalGoalTags.ConductDIY, 0.1f),
                (PersonalGoalTags.ShareTechniques, 0.05f)
            }
        },
        { ActionTags.Fabricate, new List<(string, float)>
            {
                (MilestoneTags.DevelopMaterials, 0.05f),
                (MilestoneTags.DesignBioreactors, 0.05f),
                (PersonalGoalTags.AutomateProcesses, 0.1f)
            }
        },
        { ActionTags.Collaborate, new List<(string, float)>
            {
                (MilestoneTags.FosterCooperation, 0.07f),
                (PersonalGoalTags.BuildTeam, 0.05f)
            }
        },
        { ActionTags.Experiment, new List<(string, float)>
            {
                (MilestoneTags.ConductTrials, 0.05f),
                (MilestoneTags.TestGrowthConditions, 0.05f),
                (PersonalGoalTags.PublishResearch, 0.1f)
            }
        },
        { ActionTags.Analyze, new List<(string, float)>
            {
                (MilestoneTags.IdentifyThreats, 0.05f),
                (PersonalGoalTags.UncoverChallenges, 0.1f),
                (PersonalGoalTags.EfficientSolution, 0.05f)
            }
        },
        { ActionTags.Document, new List<(string, float)>
            {
                (MilestoneTags.EstablishProtocols, 0.05f),
                (PersonalGoalTags.PublishResearch, 0.1f),
                (PersonalGoalTags.MaintainIntegrity, 0.05f)
            }
        },
        { ActionTags.Model, new List<(string, float)>
            {
                (PersonalGoalTags.EfficientSolution, 0.1f),
                (PersonalGoalTags.BalanceInnovation, 0.05f)
            }
        },
        { ActionTags.Forecast, new List<(string, float)>
            {
                (MilestoneTags.PlanProduction, 0.05f),
                (PersonalGoalTags.BalanceSustainability, 0.1f),
                (PersonalGoalTags.BalanceMission, 0.05f)
            }
        },
        { ActionTags.Trade, new List<(string, float)>
            {
                (MilestoneTags.PlanMarketEntry, 0.05f),
                (PersonalGoalTags.DevelopProfitableModel, 0.1f),
                (PersonalGoalTags.AdvocateCostCutting, 0.05f)
            }
        },
        { ActionTags.Create, new List<(string, float)>
            {
                (MilestoneTags.CreateBCI, 0.05f),
                (MilestoneTags.CreatePackaging, 0.05f),
                (PersonalGoalTags.ProveCreativity, 0.1f)
            }
        },
        { ActionTags.Exhibit, new List<(string, float)>
            {
                (PersonalGoalTags.TranslateScience, 0.1f),
                (PersonalGoalTags.ProveRelevance, 0.05f)
            }
        },
        { ActionTags.Critique, new List<(string, float)>
            {
                (PersonalGoalTags.MaintainIntegrity, 0.1f),
                (PersonalGoalTags.BalanceEthics, 0.05f)
            }
        },
        { ActionTags.Engineer, new List<(string, float)>
            {
                (MilestoneTags.DesignHydroponics, 0.05f),
                (MilestoneTags.DesignBioreactors, 0.05f),
                (PersonalGoalTags.AutomateProcesses, 0.1f)
            }
        },
        { ActionTags.Cultivate, new List<(string, float)>
            {
                (MilestoneTags.SelectCrops, 0.05f),
                (MilestoneTags.OptimizeCrops, 0.05f),
                (PersonalGoalTags.BalanceSustainability, 0.1f)
            }
        },
        { ActionTags.Automate, new List<(string, float)>
            {
                (PersonalGoalTags.AutomateProcesses, 0.1f),
                (PersonalGoalTags.EfficientSolution, 0.05f)
            }
        },
        { ActionTags.Simulate, new List<(string, float)>
            {
                (MilestoneTags.MapBrainActivity, 0.05f),
                (PersonalGoalTags.EfficientSolution, 0.1f),
                (PersonalGoalTags.BalanceInnovation, 0.05f)
            }
        },
        { ActionTags.Innovate, new List<(string, float)>
            {
                (PersonalGoalTags.EfficientSolution, 0.08f),
                (PersonalGoalTags.CommercializeInnovation, 0.05f)
            }
        },
        { ActionTags.Explore, new List<(string, float)>
            {
                (MilestoneTags.PrepareHabitat, 0.05f),
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

    public static List<string> MilestoneTagsList = new List<string>()
    {
        MilestoneTags.ExtractDNA,
        MilestoneTags.ReconstructGenome,
        MilestoneTags.CreateEmbryos,
        MilestoneTags.EstablishEthics,
        MilestoneTags.PrepareHabitat,
        MilestoneTags.SelectCrops,
        MilestoneTags.OptimizeCrops,
        MilestoneTags.DesignHydroponics,
        MilestoneTags.TestGrowthConditions,
        MilestoneTags.DeploySpaceFood,
        MilestoneTags.MapBrainActivity,
        MilestoneTags.CreateBCI,
        MilestoneTags.ConductTrials,
        MilestoneTags.AddressEthics,
        MilestoneTags.PlanProduction,
        MilestoneTags.DevelopMaterials,
        MilestoneTags.DesignFashion,
        MilestoneTags.ScaleProduction,
        MilestoneTags.CreatePackaging,
        MilestoneTags.LaunchMarketing,
        MilestoneTags.OptimizeMeatTechniques,
        MilestoneTags.DesignBioreactors,
        MilestoneTags.ReduceCosts,
        MilestoneTags.EnsureCompliance,
        MilestoneTags.PlanMarketEntry,
        MilestoneTags.IdentifyThreats,
        MilestoneTags.CreateDetection,
        MilestoneTags.EstablishProtocols,
        MilestoneTags.IntegrateDefenses,
        MilestoneTags.FosterCooperation
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
            if (PersonalGoalTagsList.Contains(tag) || MilestoneTagsList.Contains(tag))
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