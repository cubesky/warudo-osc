using OscCore;
using OscCore.Address;
using System;
using System.Collections.Generic;
using Warudo.Core.Attributes;
using Warudo.Core.Plugins;

[PluginType(Id = "party.liyin.osc", Name = "LIYIN_OSC_PLUGIN_NAME", Version = "0.2.6", Author = "Barinzaya & LiYin", Description = "LIYIN_OSC_PLUGIN_OSC_DESCRIPTION",
    NodeTypes = new[] { typeof(OscInputNode), typeof(OscSendNode) })]
public class OscInputPlugin : Plugin {
    [DataInput]
    [Label("LIYIN_OSC_PLUGIN_OSC_SERVER_PORT")]
    public int OSC_SERVER_PORT = 19190;

    private OscListener listener;

    public delegate void OscMessageHandler(OscMessage message);
    private Dictionary<string, HashSet<OscMessageHandler>> handlers = new();

    protected override void OnCreate() {
        base.OnCreate();
        Watch(nameof(OSC_SERVER_PORT), () => {
            listener.Dispose();
            listener = new(OSC_SERVER_PORT);
        });
        listener = new(OSC_SERVER_PORT);
    }

    protected override void OnDestroy() {
        listener.Dispose();
        base.OnDestroy();
    }

    public override void OnPreUpdate() {
        base.OnPreUpdate();

        while (listener.TryGetPacket(out var packet)) {
            DispatchPacket(packet);
        }
    }

    public bool AddHandler(string address, OscMessageHandler action) {
        HashSet<OscMessageHandler> addressHandlers;
        if (!handlers.TryGetValue(address, out addressHandlers)) {
            addressHandlers = new();
            handlers[address] = addressHandlers;
        }

        return addressHandlers.Add(action);
    }

    public bool RemoveHandler(string address, OscMessageHandler action) {
        HashSet<OscMessageHandler> addressHandlers;
        if (!handlers.TryGetValue(address, out addressHandlers)) {
            return false;
        }

        var result = addressHandlers.Remove(action);

        if (result && addressHandlers.Count == 0) {
            handlers.Remove(address);
        }

        return result;
    }

    private void DispatchPacket(OscPacket packet) {
        switch (packet) {
            case OscBundle bundle:
                for (var i = 0; i < bundle.Count; i++) {
                    DispatchPacket(bundle[i]);
                }
                break;

            case OscMessage message:
                DispatchMessage(message);
                break;
        }
    }

    private void DispatchMessage(OscMessage message) {
        OscAddress address;
        try {
            address = new(message.Address);
        } catch (ArgumentException) {
            return;
        }

        if (address.IsLiteral) {
            HashSet<OscMessageHandler> addressHandlers;
            if (handlers.TryGetValue(message.Address, out addressHandlers)) {
                foreach (var handler in addressHandlers) {
                    handler(message);
                }
            }
        } else {
            foreach (var pair in handlers) {
                if (address.Match(pair.Key)) {
                    foreach (var handler in pair.Value) {
                        handler(message);
                    }
                }
            }
        }
    }
}
