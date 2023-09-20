// Copyright (c) MyIA. All rights reserved.


/// <summary>
/// Represents a text completion provider instance with the corresponding given name and settings.
/// </summary>
public class OobaboogaConnectorConfiguration
{
    public string? EndPoint { get; set; } = "http://localhost";

    public int BlockingPort { get; set; } = 5000;

    public int StreamingPort { get; set; } = 5005;

}
