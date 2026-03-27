using System;
using System.Collections.Concurrent;


namespace Seq.App.TelegramNotifier
{
    public class Throttling<T> where T : notnull
    {
        private readonly ConcurrentDictionary<T, DateTime> _lastSeen = new ConcurrentDictionary<T, DateTime>();

        public bool TryBegin(T key, TimeSpan period)
        {
            if (period <= TimeSpan.Zero)
            {
                return true;
            }
            bool isAllowed = false;
            _lastSeen.AddOrUpdate(key, delegate
            {
                isAllowed = true;
                return DateTime.Now;
            }, delegate (T _, DateTime lastTime)
            {
                DateTime now = DateTime.Now;
                if (now - lastTime > period)
                {
                    isAllowed = true;
                    return now;
                }
                return lastTime;
            });
            return isAllowed;
        }
    }
}
