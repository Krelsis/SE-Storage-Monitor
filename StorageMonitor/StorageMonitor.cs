using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common;
//using Sandbox.Common.Components;
//using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game;
using VRageMath;


namespace StorageMonitor
{

    class Runtime
    {
        public static UpdateFrequency UpdateFrequency;
    }

    class API : Runtime
    {
        public IMyGridTerminalSystem GridTerminalSystem;
        public IMyProgrammableBlock Me;

        public void Echo(string message)
        {
            return;
        }
    }

    class Program : API
    {
        // Script Name (Programmable block gets appended with this)
        public static string ScriptName = " [StorageMonitor]";

        // Time of last check
        public DateTime balanceTime = DateTime.Now;

        // Lists
        public List<IMyTerminalBlock> CargoContainerList = new List<IMyTerminalBlock>();
        public List<IMyTerminalBlock> LcdList = new List<IMyTerminalBlock>();
        public List<IMyCockpit> CockpitList = new List<IMyCockpit>();
        public List<IMyFunctionalBlock> FunctionalBlockList = new List<IMyFunctionalBlock>();

        // Container Doubles
        public double CurrentMassAll = 0;
        public double CurrentVolumeAll = 0;
        public double MaxVolumeAll = 0;

        // Container Strings
        public string StringCurrentMassAll = "";
        public string StringCurrentVolumeAll = "";
        public string StringMaxVolumeAll = "";

        // Percentage Strings
        public string StringCurrentVolumeAllPerc = "";

        // Summary Strings
        public string StorageSummary = "";
        public static string DashSeparator = "-------------------------------------------------------------";

        // Keywords
        public static string IgnoreKeyword = "[StorageIgnore]";
        public static Dictionary<string, string> LCDKeywordsMap;


        //Group/Responsibility action stuff
        public static string ResponsibilityTagKeyword = "[StorageResponsibility]";
        public enum GroupActionKeywordsEnum
        {
            None = 0,
            OffWhenTotalStorageFull,
            OffWhenTotalStorageMoreThan,
            OffWhenTotalStorageMoreThanEqualTo,
            OnWhenTotalStorageFull,
            OnWhenTotalStorageMoreThan,
            OnWhenTotalStorageMoreThanEqualTo

        }
        public enum GroupActionEnum
        {
            None = 0,
            TurnOn,
            TurnOff,
        }
        public enum GroupActionExpression
        {
            MoreThan = 0,
            MoreThanEqualTo
        }
        public static Dictionary<GroupActionKeywordsEnum, string> GroupActionKeywordsMap;
        public static Dictionary<GroupActionKeywordsEnum, GroupActionEnum> GroupActionKeywordAction;
        public static Dictionary<GroupActionKeywordsEnum, GroupActionExpression> GroupActionsExpressionMap;
        public static Dictionary<GroupActionEnum, string> GroupActionsMap;

        // Script Responsibility
        public string Responsibility = "";


        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
        }

        void Main()
        {
            Me.CustomName += !KeywordInName(Me, ScriptName) ? ScriptName : "";
            CheckBlockNameKeywordCapitalisation(Me, ScriptName);

            Responsibility = DetermineResponsibility(Me);

            HandleContainers();

            LCDKeywordsMap = new Dictionary<string, string>
            {
                { "[StorageStatus]", StorageSummary},
                { "[StorageSummary]", StorageSummary},
                { "[StorageMass]", StringCurrentMassAll},
                { "[StorageVolume]", StringCurrentVolumeAll},
                { "[StorageMaxVolume]", StringMaxVolumeAll}
            };
            GroupActionKeywordsMap = new Dictionary<GroupActionKeywordsEnum, string>
            {
                { GroupActionKeywordsEnum.OffWhenTotalStorageFull, "[OffWhenTotalStorageFull]" },
                { GroupActionKeywordsEnum.OffWhenTotalStorageMoreThan, "[OffWhenTotalStorageMoreThan]" },
                { GroupActionKeywordsEnum.OffWhenTotalStorageMoreThanEqualTo, "[OffWhenTotalStorageMoreThanEqualTo]" },
                { GroupActionKeywordsEnum.OnWhenTotalStorageFull, "[OnWhenTotalStorageFull]" },
                { GroupActionKeywordsEnum.OnWhenTotalStorageMoreThan, "[OnWhenTotalStorageMoreThan]" },
                { GroupActionKeywordsEnum.OnWhenTotalStorageMoreThanEqualTo, "[OnWhenTotalStorageMoreThanEqualTo]" }
            };
            GroupActionKeywordAction = new Dictionary<GroupActionKeywordsEnum, GroupActionEnum> 
            {
                { GroupActionKeywordsEnum.OffWhenTotalStorageFull, GroupActionEnum.TurnOff },
                { GroupActionKeywordsEnum.OffWhenTotalStorageMoreThan, GroupActionEnum.TurnOff },
                { GroupActionKeywordsEnum.OffWhenTotalStorageMoreThanEqualTo, GroupActionEnum.TurnOff },
                { GroupActionKeywordsEnum.OnWhenTotalStorageFull, GroupActionEnum.TurnOn },
                { GroupActionKeywordsEnum.OnWhenTotalStorageMoreThan, GroupActionEnum.TurnOn },
                { GroupActionKeywordsEnum.OnWhenTotalStorageMoreThanEqualTo, GroupActionEnum.TurnOn }
            };
            GroupActionsMap = new Dictionary<GroupActionEnum, string>
            {
                { GroupActionEnum.TurnOff, "OnOff_Off" },
                { GroupActionEnum.TurnOn, "OnOff_On" }
            };
            GroupActionsExpressionMap = new Dictionary<GroupActionKeywordsEnum, GroupActionExpression>
            {
                { GroupActionKeywordsEnum.OffWhenTotalStorageFull, GroupActionExpression.MoreThanEqualTo },
                { GroupActionKeywordsEnum.OffWhenTotalStorageMoreThan, GroupActionExpression.MoreThan },
                { GroupActionKeywordsEnum.OffWhenTotalStorageMoreThanEqualTo, GroupActionExpression.MoreThanEqualTo },
                { GroupActionKeywordsEnum.OnWhenTotalStorageFull, GroupActionExpression.MoreThanEqualTo },
                { GroupActionKeywordsEnum.OnWhenTotalStorageMoreThan, GroupActionExpression.MoreThan },
                { GroupActionKeywordsEnum.OnWhenTotalStorageMoreThanEqualTo, GroupActionExpression.MoreThanEqualTo }
            };

            HandleLCDs();
            HandleCockpits();

            HandleAllMiscBlocks();
            WriteStorageSummaryToProgrammeBlockDisplay();

            SetProgramBlockScreenText(Me as IMyProgrammableBlock, 0, StorageSummary);

        }

        string DetermineResponsibility(IMyTerminalBlock Block)
        {
            var responsibility = FindTaggedDataForKeyword(Block, ResponsibilityTagKeyword);

            if (string.IsNullOrWhiteSpace(responsibility))
            {
                responsibility = "Default";
            }

            return responsibility;
        }

        void HandleAllMiscBlocks()
        {
            GridTerminalSystem.GetBlocksOfType(FunctionalBlockList, IsRelevantMiscOnLocalGrid);

            if (FunctionalBlockList.Count > 0)
            {
                foreach (var FunctionalBlock in FunctionalBlockList)
                {
                    foreach (var Keyword in GroupActionKeywordsMap)
                    {
                        if (HasKeywordAndNotIgnored(FunctionalBlock, Keyword.Value))
                        {
                            ProcessMiscBlock(FunctionalBlock, Keyword.Value);
                        }
                    }
                }
                StorageSummary += $@"Managing {FunctionalBlockList.Count} miscellaneous devices
                    {DashSeparator}
                ";

                StorageSummary = StorageSummary.Replace("                    ", "");
                StorageSummary = StorageSummary.Replace("                ", "");
            }
        }

        void ProcessMiscBlock(IMyTerminalBlock terminalBlock, string Keyword)
        {
            var Percentage = ToPercentage(CurrentVolumeAll, MaxVolumeAll);
            var DoAction = false;
            GroupActionEnum ActionToPerform = GroupActionEnum.None;
            GroupActionEnum InverseActionToPerform = GroupActionEnum.None;
            int PercentileCriteria = 0;
            var StrippedKeyword = Keyword.Replace("[", "");
            StrippedKeyword = StrippedKeyword.Replace("]", "");
            GroupActionKeywordsEnum KeywordEnum = GroupActionKeywordsEnum.None;


            if (GroupActionKeywordsMap.ContainsValue(Keyword))
            {
                DoAction = true;
                KeywordEnum = (GroupActionKeywordsEnum)Enum.Parse(typeof(GroupActionKeywordsEnum), StrippedKeyword);

                if (GroupActionKeywordAction[KeywordEnum] == GroupActionEnum.TurnOff)
                {
                    ActionToPerform = GroupActionEnum.TurnOff;
                    InverseActionToPerform = GroupActionEnum.TurnOn;
                }
                else if (GroupActionKeywordAction[KeywordEnum] == GroupActionEnum.TurnOn)
                {
                    ActionToPerform = GroupActionEnum.TurnOn;
                    InverseActionToPerform = GroupActionEnum.TurnOff;
                }

                var fullKeywords = new List<string>
                {
                    GroupActionKeywordsMap[GroupActionKeywordsEnum.OffWhenTotalStorageFull],
                    GroupActionKeywordsMap[GroupActionKeywordsEnum.OnWhenTotalStorageFull]
                };
                if (fullKeywords.Contains(Keyword))
                {
                    PercentileCriteria = 98;
                }
                else
                {
                    PercentileCriteria = FindFirstNumberForKeyword(terminalBlock, Keyword);
                }
            }


            if (DoAction)
            {
                var ApplyAction = false;
                var DesiredAction = "";
                if (GroupActionsExpressionMap[KeywordEnum] == GroupActionExpression.MoreThan)
                {
                    ApplyAction = true;
                    DesiredAction = Percentage > PercentileCriteria ? GroupActionsMap[ActionToPerform] : GroupActionsMap[InverseActionToPerform];
                }
                else if (GroupActionsExpressionMap[KeywordEnum] == GroupActionExpression.MoreThanEqualTo)
                {
                    ApplyAction = true;
                    DesiredAction = Percentage >= PercentileCriteria ? GroupActionsMap[ActionToPerform] : GroupActionsMap[InverseActionToPerform];
                }

                if (ApplyAction)
                {
                    var ActionList = new List<ITerminalAction>();
                    terminalBlock.GetActions(ActionList, (x) => x.Id.Equals(DesiredAction));
                    if (ActionList.Count > 0)
                    {
                        ActionList[0].Apply(terminalBlock);
                    }
                }
            }
        }

        void HandleLCDs()
        {
            GridTerminalSystem.GetBlocksOfType(LcdList, IsRelevantLCDsOnLocalGrid);

            if (LcdList.Count > 0)
            {
                foreach (var lcd in LcdList)
                {
                    foreach (var Keyword in LCDKeywordsMap)
                    {
                        if (HasKeywordAndNotIgnored(lcd, Keyword.Key))
                        {
                            SetLCDText(lcd, Keyword.Key, Keyword.Value);
                        }
                    }
                }
            }

        }

        void HandleCockpits()
        {
            GridTerminalSystem.GetBlocksOfType(CockpitList, IsRelevantCockpitsOnLocalGrid);

            if (CockpitList.Count > 0)
            {
                foreach (var cockpit in CockpitList)
                {
                    foreach (var Keyword in LCDKeywordsMap)
                    {
                        if (HasKeywordAndNotIgnored(cockpit, Keyword.Key))
                        {
                            var screenToChange = FindFirstNumberForKeyword(cockpit, Keyword.Key);
                            
                            if (screenToChange != -1)
                            {
                                SetCockpitScreenText(cockpit, screenToChange, Keyword.Value);
                            }
                        }
                    }
                }
            }
        }

        void HandleContainers()
        {
            List<IMyTerminalBlock> containersTemp = new List<IMyTerminalBlock>();
            Echo("Searching container devices.");
            GridTerminalSystem.GetBlocksOfType(containersTemp, IsContainerOnLocalGridNotIgnored);


            if (containersTemp.Count == 0)
            {
                Echo("No containers found.");
            }
            else
            {
                Echo($"Found {containersTemp.Count} containers.");
            }

            // Order the batteries from lowest to highest charge
            TimeSpan balanceTimeSpan = DateTime.Now - balanceTime;
            if (balanceTimeSpan.TotalSeconds >= 1 || CargoContainerList.Count != containersTemp.Count)
            {
                CargoContainerList = containersTemp;

                CheckContainers();
                SetupContainerStringVariables();

                balanceTime = DateTime.Now;
            }
        }

        bool IsContainerOnLocalGridNotIgnored(IMyTerminalBlock container)
        {
            if (container.IsSameConstructAs(Me))
            {
                if (!IsIgnored(container))
                {
                    try
                    {
                        var potentialContainer = container as VRage.Game.ModAPI.Ingame.IMyEntity;
                        if (potentialContainer.HasInventory)
                        {
                            if (string.IsNullOrWhiteSpace(Responsibility) || Responsibility == "Default" || DetermineResponsibility(container) == Responsibility)
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                        
                    }
                    catch
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        bool IsRelevantCockpitsOnLocalGrid(IMyTerminalBlock cockpit)
        {
            if (cockpit.IsSameConstructAs(Me))
            {
                var KeywordPresent = false;
                foreach (var Keyword in LCDKeywordsMap)
                {
                    if (HasKeywordAndNotIgnored(cockpit, Keyword.Key))
                    {
                        if (string.IsNullOrWhiteSpace(Responsibility) || Responsibility == "Default" || DetermineResponsibility(cockpit) == Responsibility)
                        {
                            KeywordPresent = true;
                        }
                    }
                }
                if (KeywordPresent)
                {
                    return true;
                }
            }
            return false;
        }

        bool IsRelevantLCDsOnLocalGrid(IMyTerminalBlock lcd)
        {
            if (lcd.IsSameConstructAs(Me))
            {
                var KeywordPresent = false;
                foreach (var Keyword in LCDKeywordsMap)
                {
                    if (HasKeywordAndNotIgnored(lcd, Keyword.Key))
                    {
                        if (string.IsNullOrWhiteSpace(Responsibility) || Responsibility == "Default" || DetermineResponsibility(lcd) == Responsibility)
                        {
                            KeywordPresent = true;
                        }
                    }
                }
                if (KeywordPresent)
                {
                    return true;
                }
            }
            return false;
        }

        bool IsRelevantMiscOnLocalGrid(IMyFunctionalBlock FunctionalBlock)
        {
            if (FunctionalBlock.IsSameConstructAs(Me) && FunctionalBlock != Me && !CargoContainerList.Contains(FunctionalBlock) && !LcdList.Contains(FunctionalBlock))
            {
                if (HasKeywordAndNotIgnored(FunctionalBlock, ScriptName.Trim()))
                {
                    if (DetermineResponsibility(FunctionalBlock) == Responsibility)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        void CheckContainers()
        {
            double _currentMass = 0;
            double _currentVolume = 0;
            double _maxVolume = 0;

            foreach (var container in CargoContainerList)
            {
                VRage.Game.ModAPI.Ingame.IMyInventory inventory;
                double _thisContainerCurrentMass = 0;
                double _thisContainerCurrentVolume = 0;
                double _thisContainerMaxVolume = 0;

                for (int i = 0; i < container.InventoryCount; i++)
                {
                    inventory = container.GetInventory(i);
                    _thisContainerCurrentMass += inventory.CurrentMass.RawValue;
                    _thisContainerCurrentVolume += inventory.CurrentVolume.RawValue;
                    _thisContainerMaxVolume += inventory.MaxVolume.RawValue;
                }

                _currentMass += _thisContainerCurrentMass;
                _currentVolume += _thisContainerCurrentVolume;
                _maxVolume += _thisContainerMaxVolume;

                RenameContainer(container, _thisContainerCurrentVolume, _thisContainerMaxVolume);
            }

            CurrentMassAll = Math.Round(_currentMass, 2);
            CurrentVolumeAll = Math.Round(_currentVolume, 2);
            MaxVolumeAll = Math.Round(_maxVolume, 2);
        }

        void SetupContainerStringVariables()
        {
            StringCurrentVolumeAllPerc = $" ( {ToPercentage(CurrentVolumeAll, MaxVolumeAll)}% )";

            StringCurrentMassAll = $"Total Mass : {GetMassRepresentation(CurrentMassAll)}";
            StringCurrentVolumeAll = $"Current Volume : {GetVolumeRepresentation(CurrentVolumeAll)}{StringCurrentVolumeAllPerc}";
            StringMaxVolumeAll = $"Max Volume : {GetVolumeRepresentation(MaxVolumeAll)}";

            StorageSummary = $@"{DashSeparator}
                    {StringCurrentMassAll}
                    {StringCurrentVolumeAll}
                    {StringMaxVolumeAll}
                    {DashSeparator}
                    Responsibility :  {Responsibility}
                    {DashSeparator}
                ";


            StorageSummary = StorageSummary.Replace("                    ", "");
            StorageSummary = StorageSummary.Replace("                ", "");

        }

        void WriteStorageSummaryToProgrammeBlockDisplay()
        {
            Echo(StorageSummary.Replace(DashSeparator, "----------------------------------"));
        }

        public void RenameContainer(IMyTerminalBlock Container, double CurrentVolume, double MaxVolume)
        {

            string newName = Container.CustomName;
            string oldStatus = System.Text.RegularExpressions.Regex.Match(Container.CustomName, @" *\((.*?)\)").Value;
            var percentage = ToPercentage(CurrentVolume, MaxVolume);

            if (oldStatus != String.Empty)
            {
                newName = Container.CustomName.Replace(oldStatus, "");
            }

            newName = $"{newName} ( {percentage}% )";

            // Rename the block if the name has changed
            if (Container.CustomName != newName)
            {
                Container.CustomName = newName;
            }

        }

        double ToPercentage(double value, double comparisonValue)
        {

            if (comparisonValue == 0)
            {
                return 0;
            }

            double percentage = Math.Round(value / comparisonValue * 100, 2);
            return percentage;
        }

        void SetLCDText(IMyTerminalBlock lcd, string title, string text)
        {
            try
            {
                IMyTextSurface lcdControl = lcd as IMyTextSurface;
                lcdControl.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                var lcdBlock = lcd as IMyTextPanel;
                lcdBlock.WritePublicTitle(title);
                lcdControl.WriteText(text);
            }
            catch
            {

            }
        }

        void SetCockpitScreenText(IMyCockpit cockpit, int screenNumber, string text)
        {
            try
            {
                if (screenNumber + 1 <= cockpit.SurfaceCount)
                {
                    IMyTextSurface lcdControl = cockpit.GetSurface(screenNumber);
                    lcdControl.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    lcdControl.WriteText(text);
                }
            }
            catch
            {

            }

        }

        void SetProgramBlockScreenText(IMyProgrammableBlock block, int screenNumber, string text)
        {
            try
            {
                if (screenNumber + 1 <= block.SurfaceCount)
                {
                    IMyTextSurface lcdControl = block.GetSurface(screenNumber);
                    lcdControl.ContentType = VRage.Game.GUI.TextPanel.ContentType.TEXT_AND_IMAGE;
                    lcdControl.WriteText(text);
                }
            }
            catch
            {

            }

        }

        int FindFirstNumberForKeyword(IMyTerminalBlock Block, string Keyword)
        {
            var attachedNumber = 0;
            if (KeywordInName(Block, Keyword))
            {
                var endOfKeyword = GetKeywordEndPos(Block.CustomName, Keyword);
                attachedNumber = FindFirstNumberInString(Block.CustomName.Substring(endOfKeyword, Block.CustomName.Length - endOfKeyword));
            }
            else if (KeywordInData(Block, Keyword))
            {
                var endOfKeyword = GetKeywordEndPos(Block.CustomData, Keyword);
                attachedNumber = FindFirstNumberInString(Block.CustomData.Substring(endOfKeyword, Block.CustomData.Length - endOfKeyword));
            }

            return attachedNumber;
        }

        int FindFirstNumberInString(string Haystack)
        {
            int number;
            string collection = "";
            bool foundFirstNumber = false;

            foreach (var character in Haystack)
            {

                if (int.TryParse(character.ToString(), out number))
                {
                    collection += character;
                    foundFirstNumber = true;
                }
                else
                {
                    if (foundFirstNumber)
                    {
                        break;
                    }
                }
            }


            if (int.TryParse(collection, out number))
            {
                return number;
            }
            return -1;
        }

        string FindTaggedDataForKeyword(IMyTerminalBlock Block, string Keyword)
        {
            var attachedData = "";
            if (KeywordInName(Block, Keyword))
            {
                var endOfKeyword = GetKeywordEndPos(Block.CustomName, ResponsibilityTagKeyword);
                attachedData = FindTaggedDataInString(Block.CustomName.Substring(endOfKeyword, Block.CustomName.Length - endOfKeyword));
            }
            else if (KeywordInData(Block, Keyword))
            {
                var endOfKeyword = GetKeywordEndPos(Block.CustomData, ResponsibilityTagKeyword);
                attachedData = FindTaggedDataInString(Block.CustomData.Substring(endOfKeyword, Block.CustomData.Length - endOfKeyword));
            }

            return attachedData;
        }

        string FindTaggedDataInString(string Haystack)
        {
            string collection = "";
            bool foundOpeningTag = false;
            bool foundClosingTag = false;

            foreach (var character in Haystack)
            {
                if (!foundOpeningTag)
                {
                    if (character == '[')
                    {
                        foundOpeningTag = true;
                    }
                }
                else
                {
                    if (character == ']')
                    {
                        foundClosingTag = true;
                        break;
                    }
                    else
                    {
                        collection += character;
                    }
                }
            }

            return foundClosingTag ? collection : "";
        }

        bool HasKeywordAndNotIgnored(IMyTerminalBlock Block, string Keyword)
        {
            if (KeywordInName(Block, Keyword) || KeywordInData(Block, Keyword))
            {
                if (KeywordInName(Block, Keyword))
                {
                    CheckBlockNameKeywordCapitalisation(Block, Keyword);
                }
                else if (KeywordInData(Block, Keyword))
                {
                    CheckBlockDataKeywordCapitalisation(Block, Keyword);
                }
                if (!IsIgnored(Block))
                {
                    return true;
                }
            }
            return false;
        }

        bool KeywordInName(IMyTerminalBlock Block, string Keyword)
        {
            if (Block.CustomName.ToLower().Contains(Keyword.ToLower()))
            {
                return true;
            }
            return false;
        }

        bool KeywordInData(IMyTerminalBlock Block, string Keyword)
        {
            if (Block.CustomData.ToLower().Contains(Keyword.ToLower()))
            {
                return true;
            }
            return false;
        }

        bool IsIgnored(IMyTerminalBlock Block)
        {
            if (KeywordInName(Block, IgnoreKeyword) || KeywordInData(Block, IgnoreKeyword))
            {
                if (KeywordInName(Block, IgnoreKeyword))
                {
                    CheckBlockNameKeywordCapitalisation(Block, IgnoreKeyword);
                }
                else if (KeywordInData(Block, IgnoreKeyword))
                {
                    CheckBlockDataKeywordCapitalisation(Block, IgnoreKeyword);
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        void CheckBlockNameKeywordCapitalisation(IMyTerminalBlock Block, string Keyword)
        {
            if (KeywordInName(Block, Keyword) && !Block.CustomName.Contains(Keyword))
            {
                var keywordStartsAt = GetKeywordStartPos(Block.CustomName.ToLower(), Keyword.ToLower());
                var keywordEndsAt = GetKeywordEndPos(Block.CustomName.ToLower(), Keyword.ToLower());
                var nameArray = Block.CustomName.ToArray();
                var keywordArray = Keyword.ToArray();

                var iterations = 0;
                for (int i = keywordStartsAt; i < keywordEndsAt; i++)
                {
                    nameArray[i] = keywordArray[iterations];
                    iterations++;
                }

                var newName = new string(nameArray);
                Block.CustomName = newName;
            }
        }

        void CheckBlockDataKeywordCapitalisation(IMyTerminalBlock Block, string Keyword)
        {
            if (KeywordInData(Block, Keyword) && !Block.CustomData.Contains(Keyword))
            {
                var keywordStartsAt = GetKeywordStartPos(Block.CustomData.ToLower(), Keyword.ToLower());
                var keywordEndsAt = GetKeywordEndPos(Block.CustomData.ToLower(), Keyword.ToLower());
                var dataArray = Block.CustomData.ToArray();
                var keywordArray = Keyword.ToArray();

                var iterations = 0;
                for (int i = keywordStartsAt; i < keywordEndsAt; i++)
                {
                    dataArray[i] = keywordArray[iterations];
                    iterations++;
                }

                var newData = new string(dataArray);
                Block.CustomData = newData;

            }
        }

        int GetKeywordStartPos(string Haystack, string Keyword)
        {
            return Haystack.IndexOf(Keyword);
        }

        int GetKeywordEndPos(string Haystack, string Keyword)
        {
            return Haystack.Length - (new string(Haystack.Reverse().ToArray())).LastIndexOf(new string(Keyword.Reverse().ToArray()));
        }

        string Pluralise(double ValueToCheck)
        {
            return ValueToCheck > 1 ? "s" : "";
        }

        string GetMassRepresentation(double MassInMilliGrams)
        {
            //Smaller Units

            //Bigger Units
            var MassInGrams = MassInMilliGrams / 1000;
            var MassInKiloGrams = MassInGrams / 1000;
            var MassInMegaGrams = MassInKiloGrams / 1000;
            var MassInGigaGrams = MassInMegaGrams / 1000;
            var MassInTeraGrams = MassInGigaGrams / 1000;
            var MassInPetaGrams = MassInTeraGrams / 1000;
            var MassInExaGrams = MassInTeraGrams / 1000;
            var MassInZettaGrams = MassInExaGrams / 1000;

            double Value = 0;
            string Unit = "G";
            string UnitPrefix = "";

            if (MassInZettaGrams >= 1)
            {
                Value = MassInZettaGrams;
                UnitPrefix = "Z";
            }
            else if (MassInExaGrams >= 1)
            {
                Value = MassInExaGrams;
                UnitPrefix = "E";
            }
            else if (MassInPetaGrams >= 1)
            {
                Value = MassInPetaGrams;
                UnitPrefix = "P";
            }
            else if (MassInTeraGrams >= 1)
            {
                Value = MassInTeraGrams;
                UnitPrefix = "T";
            }
            else if (MassInGigaGrams >= 1)
            {
                Value = MassInGigaGrams;
                UnitPrefix = "G";
            }
            else if (MassInMegaGrams >= 1)
            {
                Value = MassInMegaGrams;
                UnitPrefix = "M";
            }
            else if (MassInKiloGrams >= 1)
            {
                Value = MassInKiloGrams;
                UnitPrefix = "K";
            }
            else
            {
                Value = MassInGrams;
                UnitPrefix = "";
            }

            return $"{Math.Round(Value, 2)} {UnitPrefix}{Unit}{Pluralise(Value)}";
        }

        string GetVolumeRepresentation(double VolumeInLitres)
        {
            //Smaller Units
            var VolumeInMilliLitres = VolumeInLitres * 1000;
            //Bigger Units
            var VolumeInKiloLitres = VolumeInLitres / 1000;
            var VolumeInMegaLitres = VolumeInKiloLitres / 1000;
            var VolumeInGigaLitres = VolumeInMegaLitres / 1000;
            var VolumeInTeraLitres = VolumeInGigaLitres / 1000;
            var VolumeInPetaLitres = VolumeInTeraLitres / 1000;
            var VolumeInExaLitres = VolumeInTeraLitres / 1000;
            var VolumeInZettaLitres = VolumeInExaLitres / 1000;

            double Value = 0;
            string Unit = "L";
            string UnitPrefix = "";

            if (VolumeInZettaLitres >= 1)
            {
                Value = VolumeInZettaLitres;
                UnitPrefix = "Z";
            }
            else if (VolumeInExaLitres >= 1)
            {
                Value = VolumeInExaLitres;
                UnitPrefix = "E";
            }
            else if (VolumeInPetaLitres >= 1)
            {
                Value = VolumeInPetaLitres;
                UnitPrefix = "P";
            }
            else if (VolumeInTeraLitres >= 1)
            {
                Value = VolumeInTeraLitres;
                UnitPrefix = "T";
            }
            else if (VolumeInGigaLitres >= 1)
            {
                Value = VolumeInGigaLitres;
                UnitPrefix = "G";
            }
            else if (VolumeInMegaLitres >= 1)
            {
                Value = VolumeInMegaLitres;
                UnitPrefix = "M";
            }
            else if (VolumeInKiloLitres >= 1)
            {
                Value = VolumeInKiloLitres;
                UnitPrefix = "K";
            }
            else if (VolumeInLitres >= 1)
            {
                Value = VolumeInLitres;
                UnitPrefix = "";
            }
            else
            {
                Value = VolumeInMilliLitres;
                UnitPrefix = "m";
            }

            return $"{Math.Round(Value, 2)} {UnitPrefix}{Unit}{Pluralise(Value)}";
        }

    }
}
