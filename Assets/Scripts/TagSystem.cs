using UnityEngine;
using System.Collections.Generic;

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
        public const string SecureFunding = "SecureFunding";
        public const string MaintainIntegrity = "MaintainIntegrity";
        public const string PrioritizeWellbeing = "PrioritizeWellbeing";
        public const string FastTrackApplications = "FastTrackApplications";
        public const string NavigatePrivacy = "NavigatePrivacy";
        public const string EstablishEthics = "EstablishEthics";
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

    public static Dictionary<string, List<string>> ActionToTags = new Dictionary<string, List<string>>
    {
        { ActionTags.Treat, new List<string> { ChallengeTags.ConductTrials, ChallengeTags.EnsureCompliance, PersonalGoalTags.PrioritizeWellbeing } },
        { ActionTags.Research, new List<string> { ChallengeTags.ExtractDNA, ChallengeTags.ReconstructGenome, ChallengeTags.IdentifyThreats, ChallengeTags.InnovateProcess, PersonalGoalTags.PublishResearch } },
        { ActionTags.Consult, new List<string> { ChallengeTags.EstablishEthics, ChallengeTags.EstablishProtocols, ChallengeTags.EthicalConsiderations, PersonalGoalTags.NavigatePrivacy } },
        { ActionTags.Debate, new List<string> { ChallengeTags.AddressEthics, ChallengeTags.FosterCooperation, ChallengeTags.EthicalConsiderations, PersonalGoalTags.ResolveConflicts } },
        { ActionTags.Strategize, new List<string> { ChallengeTags.PlanProduction, ChallengeTags.PlanMarketEntry, ChallengeTags.InnovateProcess, PersonalGoalTags.DevelopProfitableModel } },
        { ActionTags.Philosophize, new List<string> { ChallengeTags.EstablishEthics, ChallengeTags.EthicalConsiderations, PersonalGoalTags.BalanceEthics } },
        { ActionTags.Broadcast, new List<string> { ChallengeTags.LaunchMarketing, ChallengeTags.PublicOutreach, PersonalGoalTags.ProveRelevance } },
        { ActionTags.Interview, new List<string> { ChallengeTags.PublicOutreach, ChallengeTags.CollaborateAcrossFields, PersonalGoalTags.GainAccess } },
        { ActionTags.Produce, new List<string> { ChallengeTags.CreatePackaging, ChallengeTags.DeploySpaceFood, ChallengeTags.ScaleProduction, PersonalGoalTags.CommercializeInnovation } },
        { ActionTags.Ideate, new List<string> { ChallengeTags.DevelopMaterials, ChallengeTags.CreateDetection, ChallengeTags.InnovateProcess, PersonalGoalTags.EfficientSolution } },
        { ActionTags.Prototype, new List<string> { ChallengeTags.CreateBCI, ChallengeTags.DesignBioreactors, ChallengeTags.InnovateProcess, PersonalGoalTags.DevelopOpenSource } },
        { ActionTags.Pitch, new List<string> { ChallengeTags.SecureFunding, ChallengeTags.PublicOutreach, PersonalGoalTags.CommercializeInnovation } },
        { ActionTags.Tinker, new List<string> { ChallengeTags.OptimizeCrops, ChallengeTags.OptimizeMeatTechniques, ChallengeTags.InnovateProcess, PersonalGoalTags.ConductDIY } },
        { ActionTags.Fabricate, new List<string> { ChallengeTags.CreateEmbryos, ChallengeTags.DesignHydroponics, ChallengeTags.ScaleProduction, PersonalGoalTags.AutomateProcesses } },
        { ActionTags.Collaborate, new List<string> { ChallengeTags.FosterCooperation, ChallengeTags.CollaborateAcrossFields, PersonalGoalTags.BuildTeam } },
        { ActionTags.Experiment, new List<string> { ChallengeTags.TestGrowthConditions, ChallengeTags.ConductTrials, ChallengeTags.InnovateProcess, PersonalGoalTags.PublishResearch } },
        { ActionTags.Analyze, new List<string> { ChallengeTags.MapBrainActivity, ChallengeTags.IdentifyThreats, ChallengeTags.EnsureCompliance, PersonalGoalTags.MaintainIntegrity } },
        { ActionTags.Document, new List<string> { ChallengeTags.EstablishProtocols, ChallengeTags.EnsureCompliance, PersonalGoalTags.BalanceTransparency } },
        { ActionTags.Model, new List<string> { ChallengeTags.ReconstructGenome, ChallengeTags.ReduceCosts, ChallengeTags.InnovateProcess, PersonalGoalTags.DevelopProfitableModel } },
        { ActionTags.Forecast, new List<string> { ChallengeTags.PlanMarketEntry, ChallengeTags.IdentifyThreats, PersonalGoalTags.BalanceSustainability } },
        { ActionTags.Trade, new List<string> { ChallengeTags.SecureFunding, ChallengeTags.DeploySpaceFood, PersonalGoalTags.AdvocateCostCutting } },
        { ActionTags.Create, new List<string> { ChallengeTags.DevelopMaterials, ChallengeTags.CreateDetection, ChallengeTags.InnovateProcess, PersonalGoalTags.ProveCreativity } },
        { ActionTags.Exhibit, new List<string> { ChallengeTags.LaunchMarketing, ChallengeTags.PublicOutreach, PersonalGoalTags.TranslateScience } },
        { ActionTags.Critique, new List<string> { ChallengeTags.EnsureCompliance, ChallengeTags.EthicalConsiderations, PersonalGoalTags.MaintainIntegrity } },
        { ActionTags.Engineer, new List<string> { ChallengeTags.CreateBCI, ChallengeTags.IntegrateDefenses, ChallengeTags.InnovateProcess, PersonalGoalTags.EfficientSolution } },
        { ActionTags.Cultivate, new List<string> { ChallengeTags.OptimizeCrops, ChallengeTags.PrepareHabitat, ChallengeTags.CollaborateAcrossFields, PersonalGoalTags.BalanceInnovation } },
        { ActionTags.Automate, new List<string> { ChallengeTags.ScaleProduction, ChallengeTags.InnovateProcess, PersonalGoalTags.AutomateProcesses } },
        { ActionTags.Simulate, new List<string> { ChallengeTags.TestGrowthConditions, ChallengeTags.MapBrainActivity, ChallengeTags.InnovateProcess, PersonalGoalTags.AdaptEarthSolutions } },
        { ActionTags.Innovate, new List<string> { ChallengeTags.CreateDetection, ChallengeTags.DevelopMaterials, ChallengeTags.InnovateProcess, PersonalGoalTags.CommercializeInnovation } },
        { ActionTags.Explore, new List<string> { ChallengeTags.SelectCrops, ChallengeTags.IdentifyThreats, ChallengeTags.CollaborateAcrossFields, PersonalGoalTags.UncoverChallenges } }
    };

    public static List<string> GetTagsForAction(string actionName)
    {
        if (ActionToTags.TryGetValue(actionName, out List<string> tags))
        {
            return tags;
        }
        return new List<string>();
    }
}