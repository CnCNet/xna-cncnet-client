﻿using System.ServiceModel;

namespace DTAClient.Domain.Multiplayer.CnCNet;

[MessageContract(WrapperName = "AddPinhole", WrapperNamespace = "urn:dslforum-org:service:WANIPv6FirewallControl:1")]
internal readonly record struct AddPinholeRequest(
    [property: MessageBodyMember(Name = "RemoteHost")] string RemoteHost,
    [property: MessageBodyMember(Name = "RemotePort")] ushort RemotePort, // 0 = wildcard
    [property: MessageBodyMember(Name = "InternalClient")] string InternalClient,
    [property: MessageBodyMember(Name = "InternalPort")] ushort InternalPort, // 0 = wildcard
    [property: MessageBodyMember(Name = "Protocol")] ushort Protocol,  // 17 = UDP
    [property: MessageBodyMember(Name = "LeaseTime")] uint LeaseTime); // 1-86400