namespace CTStitcher.Delegates
{
    /// <summary>
    /// Function delegates
    /// </summary>
    /// <param name="message"></param>
    public delegate void UpdateUILabelDelegate(string message);
    public delegate void ProvideUIUpdateDelegate(int percentComplete, string message = "", bool fail = false);
}
