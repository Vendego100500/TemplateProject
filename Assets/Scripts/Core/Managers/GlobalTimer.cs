
using System;
using System.Collections;
using System.Diagnostics;
using Parameters;
using UnityEngine;
using Utils;

namespace Core.Managers
{
    public class GlobalTimer : Singleton<GlobalTimer>
    {
        private const int MaxUpdatePerFrame = 3;

        private readonly DateTime _serverUtc;
        private readonly Stopwatch _stopwatch;
        private readonly float _syncTime;

        private float _tickDuration;
        private float _lastUpdateTime;
        private Coroutine _timerCoroutine;

        public DateTime Now => _serverUtc.AddSeconds(Time.unscaledTime - _syncTime);
        public DateTime LocalizedNow => Now.ToLocalTime();
        public long UnixTimeS => ConvertToUnixTimestamp(Now);
        public long UnixTimeMs => ConvertToUnixTimestampMs(Now);

        public event Action Tick;

        private GlobalTimer()
        {
            _serverUtc = DateTime.Now;
            _stopwatch = new Stopwatch();
            _syncTime =  Time.unscaledTime;
        }

        public static long ConvertToUnixTimestampMs(DateTime date)
        {
            DateTime origin = new(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date - origin;
            return (long)Math.Floor(diff.TotalMilliseconds);
        }

        public static long ConvertToUnixTimestamp(DateTime date)
        {
            DateTime origin = new(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = date - origin;
            return (long)Math.Floor(diff.TotalSeconds);
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            DateTime dtDateTime = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
            return dtDateTime;
        }

        public static DateTime UnixTimeStampToDateTimeLocal(double unixTimeStamp)
        {
            return UnixTimeStampToDateTime(unixTimeStamp).ToLocalTime();
        }


        public void Start()
        {
            _tickDuration = 1000f / Mathf.Max(1, DataAssets.Instance.Game.Fps);
            _stopwatch.Start();
            
            _timerCoroutine = Routiner.Start(TimerCoroutine());
        }

        public void Stop()
        {
            Routiner.Stop(_timerCoroutine);
            _stopwatch.Stop();
        }
        

        private IEnumerator TimerCoroutine()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                Update();
            }
        }

        private void Update()
        {
            int counter = 0;
            do
            {
                float diff = _stopwatch.ElapsedMilliseconds - _lastUpdateTime;
                long frame = (long)(diff / _tickDuration);
                if (frame <= 0)
                {
                    break;
                }

                _lastUpdateTime += _tickDuration;
                Tick.InvokeSafe();
            } 
            while (++counter < MaxUpdatePerFrame);

            if (counter == MaxUpdatePerFrame)
            {
                _lastUpdateTime = _stopwatch.ElapsedMilliseconds;
            }
        }
    }
}