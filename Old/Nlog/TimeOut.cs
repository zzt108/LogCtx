using Ato.SpecFlow.Shared.Support;

namespace GreenSpeed.ATO.OnDemand.Spec.Tests.Helpers
{
    public class TimeOut
    {
        private DateTime _endTimeOut;
        private DateTime _start;
        private DateTime _lapTime;
        private double _lastIntervalMs;
        private Func<bool> _condition;
        private bool _throwException = false;
        private string _failReason = string.Empty;
        private bool _disabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeOut"/> class.
        /// </summary>
        /// <param name="intervalMs">If empty or -1 then the Timeout is disabled</param>
        /// <param name="condition"></param>
        /// <param name="throwException"></param>
        public TimeOut(double intervalMs = -1, Func<bool>? condition = null, bool throwException = false)
        {
            Disabled = intervalMs <= 0;
            Reset(intervalMs, condition);
            _throwException = throwException;
        }

        /// <summary>
        /// Sets the fail reason for the timeout.
        /// This fail reason will be used in the exception message if the timeout is expired and throwException is set to true.
        /// </summary>
        /// <param name="failReason">The fail reason for the timeout.</param>
        /// <returns>The current instance of the TimeOut class.</returns>
        public TimeOut SetFailReason(string failReason)
        {
            _failReason = failReason;
            return this;
        }

        /// <summary>
        /// Sets a flag indicating whether an exception should be thrown when the timeout expires.
        /// </summary>
        /// <param name="throwException">
        /// A boolean value indicating whether an exception should be thrown when the timeout expires.
        /// If set to true, an exception will be thrown when the timeout expires.
        /// If set to false, no exception will be thrown when the timeout expires.
        /// </param>
        /// <returns>
        /// The current instance of the TimeOut class.
        /// This allows method chaining to set multiple properties in a single line of code.
        /// </returns>
        public TimeOut SetThrow(bool throwException)
        {
            _throwException = throwException;
            return this;
        }

        /// <summary>
        /// Sets a flag indicating whether the timeout should be disabled.
        /// </summary>
        /// <param name="disabled">
        /// A boolean value indicating whether the timeout should be disabled.
        /// If set to true, the timeout will be disabled.
        /// If set to false, the timeout will be enabled.
        /// </param>
        /// <returns>
        /// The current instance of the TimeOut class.
        /// This allows method chaining to set multiple properties in a single line of code.
        /// </returns>
        public TimeOut SetDisabled(bool disabled)
        {
            Disabled = disabled;
            return this;
        }

        /// <summary>
        /// Sets a condition function to be checked before the timeout expiration.
        /// If the condition function returns true, the timeout will not expire.
        /// </summary>
        /// <param name="condition">
        /// A function that returns a boolean value.
        /// The function will be called repeatedly until it returns true or the timeout expires.
        /// </param>
        /// <returns>
        /// The current instance of the TimeOut class.
        /// This allows method chaining to set multiple properties in a single line of code.
        /// </returns>
        public TimeOut SetCondition(Func<bool> condition)
        {
            _condition = condition;
            return this;
        }

        /// <summary>
        /// reset timeout
        /// </summary>
        /// <param name="intervalMs">Timeout duration. If interval is less than 1 then disable timeout.</param>
        /// <param name="condition"></param>
        public TimeOut Reset(double intervalMs, Func<bool> condition = null)
        {
            _condition = condition;
            _lastIntervalMs = intervalMs;
            _start = DateTime.Now;
            Lap();
            _endTimeOut = _start.AddMilliseconds(intervalMs);
            return this;
        }

        /// <summary>
        /// Resets with last timeout interval.
        /// </summary>
        public void Reset()
        {
            _start = DateTime.Now;
            _endTimeOut = _start.AddMilliseconds(_lastIntervalMs);
        }

        /// <summary>
        /// Records the current time as the last lap time.
        /// </summary>
        /// <returns>The current instance of the TimeOut class.</returns>
        private TimeOut Lap()
        {
            _lapTime = DateTime.Now;
            return this;
        }

        /// <summary>
        /// Gets a value indicating whether the timeout has expired.
        /// </summary>
        public bool IsOver
        {
            get
            {
                bool isOver = Expired && !Disabled;
                if (isOver && _throwException)
                {
                    throw new TimeoutException($"{_failReason} Timeout after {_lastIntervalMs} ms");
                }

                return isOver || (_condition != null && _condition());

            }
        }

        /// <summary>
        /// Indicates whether the timeout has expired.
        /// </summary>
        /// <returns>
        /// <c>true</c> if the timeout has expired; otherwise, <c>false</c>.
        /// </returns>
        public bool Expired => DateTime.Now >= Lap()._endTimeOut;

        /// <summary>
        /// Gets the time that has elapsed since the timeout was started.
        /// </summary>
        public TimeSpan Elapsed => _lapTime - _start;

        /// <summary>
        /// Gets the last interval in milliseconds.
        /// </summary>
        public double LastIntervalMs => _lastIntervalMs;

        /// <summary>
        /// Gets the start time of the timeout.
        /// </summary>
        public DateTime StartTime => _start;

        /// <summary>
        /// Gets a value indicating whether the timeout is disabled.
        /// </summary>
        public bool Disabled { get => _disabled || (_lastIntervalMs == 0); private set => _disabled = value; }

        /// <summary>
        /// Waits for the result of the timeout.
        /// </summary>
        /// <param name="resolutionMs">The time, in milliseconds, to wait between checks for the timeout to expire.</param>
        /// <returns><c>true</c> if the timeout did not expire; otherwise, <c>false</c>.</returns>
        public bool WaitForResult(int resolutionMs, int disabledTimeOutMS = 3000)
        {
            // if the timeout is disabled then wait for disabledTimeOutMS and return the condition.
            if (Disabled)
            {
                Thread.Sleep(disabledTimeOutMS);
                LogCtx.Logger?.Warn($"{_failReason} Timeout disabled");
                return _condition();
            }

            // Wait for the specified interval and check if the timeout has expired.
            while (!IsOver)
            {
                Thread.Sleep(resolutionMs);
            }

            return !Expired;
        }
    }
}
