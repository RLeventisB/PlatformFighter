using System;
using System.Runtime.CompilerServices;

namespace PlatformFighter.Rendering
{
    public interface ITimedAction<T> where T : Delegate
    {
        public T Action { get; set; }
        public float Interval { get; set; }
        public float Counter { get; set; }
        public void Update();
        public void Update(in float timeDelta);
    }
    public class TimedAction : ITimedAction<TimedAction.TimedActionDelegate>
    {
        public TimedAction(TimedActionDelegate action, float interval, float initialCounter = 0)
        {
            Action = action;
            Interval = interval;
            Counter = initialCounter;
        }
        public TimedAction()
        {
            Action = delegate { };
            Interval = 0;
            Counter = 0;
        }
        public void Update()
        {
            UpdateLoop();
            Counter += Renderer.TimeDelta;
        }
        public void Update(in float timeDelta)
        {
            UpdateLoop();
            Counter += timeDelta;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateLoop()
        {
            while (Counter >= Interval)
            {
                Counter -= Interval;
                float cnt = Counter;
                Action(in cnt);
            }
        }
        public TimedActionDelegate Action { get; set; }
        public float Interval { get; set; }
        public float Counter { get; set; }
        public delegate void TimedActionDelegate(in float offset);
        public override bool Equals(object obj) => obj is TimedAction action && action.Action == Action && action.Interval == Interval && action.Counter == Counter;
        public override int GetHashCode() => Interval.GetHashCode() ^ Counter.GetHashCode() ^ Action.GetHashCode();
        public static bool operator ==(TimedAction left, TimedAction right) => left.Equals(right);
        public static bool operator !=(TimedAction left, TimedAction right) => !(left == right);
        public override string ToString() => $"Method Name:{Action.Method.Name}, Interval: {Interval}, Counter: {Counter}";
    }
    public class OneUseTimedAction : ITimedAction<TimedAction.TimedActionDelegate>
    {
        public OneUseTimedAction(TimedAction.TimedActionDelegate action, float target, float initialTime = 0f)
        {
            Action = action;
            Interval = target;
            Counter = initialTime;
        }
        public OneUseTimedAction()
        {
            Action = delegate { };
            Interval = 0;
            Counter = 0;
        }
        public void Update()
        {
            if (!HasProcessed)
            {
                UpdateLoop();
                Counter += Renderer.TimeDelta;
            }
        }
        public void Update(in float timeDelta)
        {
            if (!HasProcessed)
            {
                UpdateLoop();
                Counter += timeDelta;
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateLoop()
        {
            if (Counter > Interval)
            {
                float cnt = Counter - Interval;
                Action(in cnt);
                HasProcessed = true;
            }
        }
        public bool HasProcessed { get; set; }
        public TimedAction.TimedActionDelegate Action { get; set; }
        public float Counter { get; set; }
        public float Interval { get; set; }
        public override bool Equals(object obj) => obj is OneUseTimedAction action && action.Action == Action && action.Counter == Counter && action.Interval == Interval && action.HasProcessed == HasProcessed;
        public override int GetHashCode() => Counter.GetHashCode() ^ Action.GetHashCode() & Interval.GetHashCode() ^ HasProcessed.GetHashCode();
        public static bool operator ==(OneUseTimedAction left, OneUseTimedAction right) => left.Equals(right);
        public static bool operator !=(OneUseTimedAction left, OneUseTimedAction right) => !(left == right);
        public override string ToString() => $"Method Name:{Action.Method.Name}, Timer: {Counter}, Target: {Interval}, HasProcessed: {HasProcessed}";
    }
    public struct TimedActionWithFrame : ITimedAction<TimedActionWithFrame.TimedActionDelegate>
    {
        public TimedActionWithFrame(TimedActionDelegate action, float interval, float initialCounter = 0)
        {
            Action = action;
            Interval = interval;
            Counter = initialCounter;
        }
        public TimedActionWithFrame()
        {
            Action = delegate { };
            Interval = 0;
            Counter = 0;
        }
        public void Update()
        {
            UpdateLoop();
            Counter += Renderer.TimeDelta;
        }
        public void Update(in float timeDelta)
        {
            UpdateLoop();
            Counter += timeDelta;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateLoop()
        {
            while (Counter >= Interval)
            {
                Counter -= Interval;
                Iteration++;
                float cnt = Counter;
                float it = Interval * Iteration;
                Action(in cnt, in it);
            }
        }
        public TimedActionDelegate Action { get; set; }
        public float Interval { get; set; }
        public float Counter { get; set; }
        public uint Iteration { get; set; } = 0;
        public delegate void TimedActionDelegate(in float offset, in float frame);
        public override bool Equals(object obj) => obj is TimedActionWithFrame action && action.Action == Action && action.Interval == Interval && action.Counter == Counter && action.Iteration == Iteration;
        public override int GetHashCode() => Interval.GetHashCode() ^ Counter.GetHashCode() ^ Action.GetHashCode() ^ Iteration.GetHashCode();
        public static bool operator ==(TimedActionWithFrame left, TimedActionWithFrame right) => left.Equals(right);
        public static bool operator !=(TimedActionWithFrame left, TimedActionWithFrame right) => !(left == right);
        public override string ToString() => $"Method Name:{Action.Method.Name}, Interval: {Interval}, Counter: {Counter}, Frame: {Interval * Iteration}";
    }
    public struct TimedActionWithIteration : ITimedAction<TimedAction.TimedActionDelegate>
    {
        public TimedActionWithIteration(TimedAction.TimedActionDelegate action, float interval, float initialCounter = 0)
        {
            Action = action;
            Interval = interval;
            Counter = initialCounter;
        }
        public TimedActionWithIteration()
        {
            Action = delegate { };
            Interval = 0;
            Counter = 0;
        }
        public void Update()
        {
            UpdateLoop();
            Counter += Renderer.TimeDelta;
        }
        public void Update(in float timeDelta)
        {
            UpdateLoop();
            Counter += timeDelta;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateLoop()
        {
            while (Counter >= Interval)
            {
                Counter -= Interval;
                Iteration++;
                float cnt = Counter;
                Action(in cnt);
            }
        }
        public TimedAction.TimedActionDelegate Action { get; set; }
        public float Interval { get; set; }
        public float Counter { get; set; }
        public uint Iteration { get; set; }
        public override bool Equals(object obj) => obj is TimedActionWithIteration action && action.Action == Action && action.Interval == Interval && action.Counter == Counter && action.Iteration == Iteration;
        public override int GetHashCode() => Interval.GetHashCode() ^ Counter.GetHashCode() ^ Action.GetHashCode() ^ Iteration.GetHashCode();
        public static bool operator ==(TimedActionWithIteration left, TimedActionWithIteration right) => left.Equals(right);
        public static bool operator !=(TimedActionWithIteration left, TimedActionWithIteration right) => !(left == right);
        public override string ToString() => $"Method Name:{Action.Method.Name}, Interval: {Interval}, Counter: {Counter}, Interval: {Interval}";
    }
}