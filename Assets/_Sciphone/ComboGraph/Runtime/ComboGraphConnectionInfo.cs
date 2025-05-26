using System;

namespace Sciphone.ComboGraph
{
    [Serializable]
    public class ComboGraphConnectionInfo
    {
        public ComboGraphPortInfo inputPort;
        public ComboGraphPortInfo outputPort;

        public ComboGraphConnectionInfo(ComboGraphPortInfo inputPort, ComboGraphPortInfo outputPort)
        {
            this.inputPort = inputPort;
            this.outputPort = outputPort;
        }

        public ComboGraphConnectionInfo(string inputNodeId, int inputPortIndex, string outputNodeId, int outputPortIndex)
        {
            inputPort = new ComboGraphPortInfo(inputNodeId, inputPortIndex);
            outputPort = new ComboGraphPortInfo(outputNodeId, outputPortIndex);
        }
    }

    [Serializable]
    public class ComboGraphPortInfo
    {
        public string nodeId;
        public int portIndex;

        public ComboGraphPortInfo(string nodeId, int portIndex)
        {
            this.nodeId = nodeId;   
            this.portIndex = portIndex;
        }
    }
}