using OscCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using Warudo.Core.Attributes;
using Warudo.Core.Data;
using Warudo.Core.Graphs;

[NodeType(Id = "party.liyin.osc.nodes.send", Title = "LIYIN_OSC_PLUGIN_OSC_NODE_SEND_OSC_TITLE", Category = "LIYIN_OSC_PLUGIN_OSC_GATEGORY")]
public class OscSendNode : Node
{
    [DataInput]
    [Label("LIYIN_OSC_PLUGIN_OSC_NODE_SEND_OSC_HOST")]
    public string Host = "127.0.0.1";
    [DataInput]
    [Label("LIYIN_OSC_PLUGIN_OSC_NODE_SEND_OSC_PORT")]
    public int Port = 8999;
    [DataInput]
    [Label("LIYIN_OSC_PLUGIN_OSC_ADDRESS")]
    public string Address = "/value";
    [DataInput]
    [Label("LIYIN_OSC_PLUGIN_OSC_ARGUMENTS")]
    public OscInputType[] ArgumentsProps;

    UdpClient udpClient;
    

    [FlowInput]
    public Continuation Enter()
    {
        List<object> args = new List<object>();
        for (int i = 0; i < ArgumentsProps.Length; i++)
        {
            var arg = ArgumentsProps[i];
            var inputPort = this.GetDataInputPort($"Arg_{i}");
            if (inputPort == null) continue;
            var value = inputPort.Getter();
            switch (arg)
            {
                case OscInputType.Int:
                    args.Add((int)value);
                    break;
                case OscInputType.Float:
                    args.Add((float)value);
                    break;
                case OscInputType.String:
                    args.Add((string)value);
                    break;
                case OscInputType.Boolean:
                    args.Add((bool)value);
                    break;
            }
        }
        OscMessage message = new OscMessage(Address, args.ToArray());
        OscBundle bundle = new OscBundle(OscTimeTag.Now, message);
        byte[] data = new byte[512];
        int len = bundle.Write(data, 0);
        udpClient.Send(data, len);
        return Exit;
    }

    [FlowOutput]
    public Continuation Exit;

    [Markdown]
    [HiddenIf(nameof(NeedHideStatus))]
    public string Status = "";

    public bool NeedHideStatus()
    {
        return string.IsNullOrEmpty(Status);
    }

    protected override void OnCreate()
    {
        base.OnCreate();
        udpClient = new UdpClient();
        Watch(nameof(ArgumentsProps), () =>
        {
            ConfigInputs();
        });
        Watch(nameof(Host), () => {
            ConfigClient();
        });
        Watch(nameof(Port), () => {
            ConfigClient();
        });
        ConfigInputs();
        ConfigClient();
    }

    public void ConfigClient()
    {
        if (udpClient.Client.Connected)
        {
            udpClient.Close();
        }
        try
        {
            udpClient = new UdpClient();
            udpClient.Connect(Host, Port);
            Status = "";
        }
        catch (SocketException e)
        {
            Status = $"Failed to connect to {Host}:{Port} - {e.Message}";
        }
        this.Broadcast();
    }

    public void ConfigInputs()
    {
        var ports = this.DataInputPortCollection.GetPorts().Where(it => it.Key.StartsWith("Arg_")).Select(it=>it.Key).ToList();
        foreach (var port in ports)
        {
            this.DataInputPortCollection.RemovePort(port);
        }
        this.Broadcast();
        for (int i = 0; i < ArgumentsProps.Length; i++)
        {
            OscInputType argType = ArgumentsProps[i];
            DataInputProperties dataInputProperties = new DataInputProperties();
            dataInputProperties.label = $"Arg {i}";
            dataInputProperties.order = 1000 + i;
            switch (argType)
            {
                case OscInputType.Int:
                    {
                        this.AddDataInputPort($"Arg_{i}", typeof(int), 0, dataInputProperties);
                        break;
                    }
                case OscInputType.Float:
                    { 
                        this.AddDataInputPort($"Arg_{i}", typeof(float), 0f, dataInputProperties);
                        break;
                    }
                case OscInputType.String:
                    {
                        this.AddDataInputPort($"Arg_{i}", typeof(String), "", dataInputProperties);
                        break;
                    }
                case OscInputType.Boolean:
                    {
                        this.AddDataInputPort($"Arg_{i}", typeof(bool), "", dataInputProperties);
                        break;
                    }
            }
        }
        this.Broadcast();
    }

}
