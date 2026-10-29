namespace SessionDashboard;

internal static class ProfileActivity
{
    internal static SessionRecap[] Completed(IEnumerable<DashboardSession> sessions)=>sessions.Where(s=>s.Recap!=null)
        .GroupBy(s=>s.Recap!.CompletionId!=Guid.Empty?s.Recap.CompletionId:s.SessionValuesReset && s.ContinuedFrom is {} previous?previous:s.SessionId)
        .Select(group=>group.First().Recap!).ToArray();
}
