namespace Standard.Static
{
    public static class Free
    {
        public static void UnsubscribeAllHandlers<T>(this MulticastDelegate? eventDelegate, Action<T> unsubscribeAction) where T : Delegate
        {
            if (eventDelegate is null) return;

            foreach (var handler in eventDelegate.GetInvocationList())
            {
                unsubscribeAction?.Invoke((T)handler);
            }

            return;
        }
    }
}
