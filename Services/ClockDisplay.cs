using System;
using System.Timers;

namespace RonCafeApp.Services;

public class ClockDisplay
{
    private readonly Timer _timer;
    public event Action<DateTime>? OnMinuteChanged;
    public event Action<int>? OnWarningTriggered;
    public event Action? OnCurfewReached;
    public event Action? OnCurfewLifted; 

    public ClockDisplay()
    {
        _timer = new Timer(1000);
        _timer.Elapsed += (s, e) =>
        {
            var now = DateTime.Now;
            OnMinuteChanged?.Invoke(now);

            // Warning at 9:55 PM
            if (now.Hour == 21 && now.Minute == 55 && now.Second == 0)
                OnWarningTriggered?.Invoke(5);

            // Curfew at 10:00 PM
            if (now.Hour == 22 && now.Minute == 0 && now.Second == 0)
                OnCurfewReached?.Invoke();
            if (now.Hour == 6 && now.Minute == 0 && now.Second == 0)
                OnCurfewLifted?.Invoke();
        };
        _timer.Start();
    }

    public bool IsCurrentlyInCurfewState()
    {
        var hour = DateTime.Now.Hour;
        return hour >= 22 || hour < 6;
    }
}