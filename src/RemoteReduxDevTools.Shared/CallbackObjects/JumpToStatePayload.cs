﻿namespace RemoteReduxDevTools.Shared.CallbackObjects;

public class JumpToStatePayload : BasePayload
{
#pragma warning disable IDE1006 // Naming Styles
	public int index { get; set; }
	public int actionId { get; set; }
#pragma warning restore IDE1006 // Naming Styles
}