using System;
using System.Linq;
using System.Threading.Tasks;
using SharpBrick.PoweredUp.Devices;
using SharpBrick.PoweredUp.Protocol.Formatter;
using SharpBrick.PoweredUp.Protocol.Messages;

namespace SharpBrick.PoweredUp.Knowledge
{
    public static class KnowledgeManager
    {
        public static bool ApplyStaticProtocolKnowledge(PoweredUpMessage message, ProtocolKnowledge knowledge)
        {
            var applicableMessage = true;

            PortInfo port;
            PortModeInfo mode;

            switch (message)
            {
                case PortInformationForModeInfoMessage msg:
                    port = knowledge.Port(msg.PortId);

                    port.OutputCapability = msg.OutputCapability;
                    port.InputCapability = msg.InputCapability;
                    port.LogicalCombinableCapability = msg.LogicalCombinableCapability;
                    port.LogicalSynchronizableCapability = msg.LogicalSynchronizableCapability;
                    port.Modes = Enumerable.Range(0, msg.TotalModeCount).Select(modeIndex => new PortModeInfo()
                    {
                        PortId = msg.PortId,
                        ModeIndex = (byte)modeIndex,
                        IsInput = ((1 << modeIndex) & msg.InputModes) > 0,
                        IsOutput = ((1 << modeIndex) & msg.OutputModes) > 0
                    }).ToArray();

                    break;
                case PortInformationForPossibleModeCombinationsMessage msg:
                    port = knowledge.Port(msg.PortId);

                    port.ModeCombinations = msg.ModeCombinations;
                    break;


                case PortModeInformationForNameMessage msg:
                    mode = knowledge.PortMode(msg.PortId, msg.Mode);

                    mode.Name = msg.Name;
                    break;
                case PortModeInformationForRawMessage msg:
                    mode = knowledge.PortMode(msg.PortId, msg.Mode);

                    mode.RawMin = msg.RawMin;
                    mode.RawMax = msg.RawMax;
                    break;
                case PortModeInformationForPctMessage msg:
                    mode = knowledge.PortMode(msg.PortId, msg.Mode);

                    mode.PctMin = msg.PctMin;
                    mode.PctMax = msg.PctMax;
                    break;
                case PortModeInformationForSIMessage msg:
                    mode = knowledge.PortMode(msg.PortId, msg.Mode);

                    mode.SIMin = msg.SIMin;
                    mode.SIMax = msg.SIMax;
                    break;
                case PortModeInformationForSymbolMessage msg:
                    mode = knowledge.PortMode(msg.PortId, msg.Mode);

                    mode.Symbol = msg.Symbol;
                    break;
                case PortModeInformationForMappingMessage msg:
                    mode = knowledge.PortMode(msg.PortId, msg.Mode);

                    mode.InputSupportsNull = msg.InputSupportsNull;
                    mode.InputSupportFunctionalMapping20 = msg.InputSupportFunctionalMapping20;
                    mode.InputAbsolute = msg.InputAbsolute;
                    mode.InputRelative = msg.InputRelative;
                    mode.InputDiscrete = msg.InputDiscrete;

                    mode.OutputSupportsNull = msg.OutputSupportsNull;
                    mode.OutputSupportFunctionalMapping20 = msg.OutputSupportFunctionalMapping20;
                    mode.OutputAbsolute = msg.OutputAbsolute;
                    mode.OutputRelative = msg.OutputRelative;
                    mode.OutputDiscrete = msg.OutputDiscrete;
                    break;
                case PortModeInformationForValueFormatMessage msg:
                    mode = knowledge.PortMode(msg.PortId, msg.Mode);

                    mode.NumberOfDatasets = msg.NumberOfDatasets;
                    mode.DatasetType = msg.DatasetType;
                    mode.TotalFigures = msg.TotalFigures;
                    mode.Decimals = msg.Decimals;
                    break;
                default:
                    applicableMessage = false;
                    break;
            }

            return applicableMessage;
        }
        public static Task ApplyDynamicProtocolKnowledge(PoweredUpMessage message, ProtocolKnowledge knowledge)
        {
            PortInfo port;
            PortModeInfo mode;
            switch (message)
            {
                case HubAttachedIOForAttachedDeviceMessage msg:
                    port = knowledge.Port(msg.PortId);

                    ResetProtocolKnowledgeForPort(port.PortId, knowledge);
                    port.IsDeviceConnected = true;
                    port.IOTypeId = msg.IOTypeId;
                    port.HardwareRevision = msg.HardwareRevision;
                    port.SoftwareRevision = msg.SoftwareRevision;

                    AddCachePortAndPortModeInformation(msg.IOTypeId, msg.HardwareRevision, msg.SoftwareRevision, port, knowledge);
                    break;
                case HubAttachedIOForDetachedDeviceMessage msg:
                    port = knowledge.Port(msg.PortId);

                    ResetProtocolKnowledgeForPort(port.PortId, knowledge);
                    port.IsDeviceConnected = false;
                    break;

                case PortInputFormatSingleMessage msg:
                    port = knowledge.Port(msg.PortId);
                    mode = knowledge.PortMode(msg.PortId, msg.Mode);

                    port.LastFormattedPortMode = msg.Mode;

                    mode.DeltaInterval = msg.DeltaInterval;
                    mode.NotificationEnabled = msg.NotificationEnabled;
                    break;

                case PortInputFormatCombinedModeMessage msg:
                    port = knowledge.Port(msg.PortId);

                    port.UsedCombinationIndex = msg.UsedCombinationIndex;
                    port.MultiUpdateEnabled = msg.MultiUpdateEnabled;
                    port.ConfiguredModeDataSetIndex = msg.ConfiguredModeDataSetIndex;
                    break;
            }

            return Task.CompletedTask;
        }

        private static void AddCachePortAndPortModeInformation(HubAttachedIOType type, Version hardwareRevision, Version softwareRevision, PortInfo port, ProtocolKnowledge knowledge)
        {
            var device = DeviceFactory.Create(type);

            if (device != null)
            {
                foreach (var message in device.GetStaticPortInfoMessages(hardwareRevision, softwareRevision).Select(b => MessageEncoder.Decode(b, null)))
                {
                    switch (message)
                    {
                        case PortModeInformationMessage pmim:
                            pmim.PortId = port.PortId;
                            break;
                        case PortInformationMessage pim:
                            pim.PortId = port.PortId;
                            break;
                    }

                    ApplyStaticProtocolKnowledge(message, knowledge);
                }
            }
        }

        private static void ResetProtocolKnowledgeForPort(byte portId, ProtocolKnowledge knowledge)
        {
            var port = knowledge.Port(portId);

            port.IsDeviceConnected = false;
            port.IOTypeId = HubAttachedIOType.Unknown;
            port.HardwareRevision = new Version("0.0.0.0");
            port.SoftwareRevision = new Version("0.0.0.0");

            port.OutputCapability = false;
            port.InputCapability = false;
            port.LogicalCombinableCapability = false;
            port.LogicalSynchronizableCapability = false;
            port.Modes = Array.Empty<PortModeInfo>();

            port.ModeCombinations = Array.Empty<ushort>();

            port.UsedCombinationIndex = 0;
            port.MultiUpdateEnabled = false;
            port.ConfiguredModeDataSetIndex = Array.Empty<int>();
        }
    }
}