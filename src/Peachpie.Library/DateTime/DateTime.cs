﻿using Pchp.Core;
using Pchp.Library;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System_DateTime = System.DateTime;

namespace Pchp.Library.DateTime
{
    /// <summary>
    /// Internal container for last datetime parse operation errors.
    /// </summary>
    sealed class DateTimeErrors
    {
        public static DateTimeErrors Empty { get; } = new DateTimeErrors();

        public IList<string> Warnings { get; set; }
        public IList<string> Errors { get; set; }

        public bool HasErrors => Errors != null && Errors.Count != 0;

        public PhpArray ToPhpArray()
        {
            var warnings = new PhpArray(Warnings);
            var errors = new PhpArray(Errors);

            return new PhpArray(4)
            {
                { "warning_count", warnings.Count },
                { "warnings", warnings },
                { "error_count", errors.Count },
                { "errors", errors },
            };
        }
    }

    /// <summary>
    /// Representation of date and time.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class DateTime : DateTimeInterface, IPhpComparable, IPhpCloneable
    {
        #region Constants

        public const string ATOM = DateTimeFunctions.DATE_ATOM;// @"Y-m-d\TH:i:sP";
        public const string COOKIE = DateTimeFunctions.DATE_COOKIE;// @"l, d-M-y H:i:s T";
        public const string ISO8601 = DateTimeFunctions.DATE_ISO8601;// @"Y-m-d\TH:i:sO";
        public const string RFC822 = DateTimeFunctions.DATE_RFC822;// @"D, d M y H:i:s O";
        public const string RFC850 = DateTimeFunctions.DATE_RFC850;// @"l, d-M-y H:i:s T";
        public const string RFC1036 = DateTimeFunctions.DATE_RFC1036;// @"D, d M y H:i:s O";
        public const string RFC1123 = DateTimeFunctions.DATE_RFC1123;// @"D, d M Y H:i:s O";
        public const string RFC2822 = DateTimeFunctions.DATE_RFC2822;// @"D, d M Y H:i:s O";
        public const string RFC3339 = DateTimeFunctions.DATE_RFC3339;// @"Y-m-d\TH:i:sP";
        public const string RSS = DateTimeFunctions.DATE_RSS;// @"D, d M Y H:i:s O";
        public const string W3C = DateTimeFunctions.DATE_W3C;// @"Y-m-d\TH:i:sP";

        #endregion

        #region Fields

        // dont see what these are for, no fields/props on php DateTime obj?
        //public PhpReference date = new PhpSmartReference();
        //public PhpReference timezone_type = new PhpSmartReference();
        //public PhpReference timezone = new PhpSmartReference();

        readonly protected Context _ctx;

        /// <summary>
        /// Get the date-time value, stored in UTC
        /// </summary>
        internal System_DateTime Time { get; private set; }

        /// <summary>
        /// Get the time zone for this DateTime object
        /// </summary>
        internal TimeZoneInfo TimeZone { get; private set; }

        #endregion

        #region Construction

        [PhpFieldsOnlyCtor]
        protected DateTime(Context ctx)
            : this(ctx, System_DateTime.UtcNow, PhpTimeZone.GetCurrentTimeZone(ctx))
        {
        }

        private DateTime(Context ctx, System_DateTime time, TimeZoneInfo timezone)
        {
            Debug.Assert(ctx != null);

            _ctx = ctx;

            this.Time = time;
            this.TimeZone = timezone ?? throw new ArgumentNullException(nameof(timezone));
        }

        // public __construct ([ string $time = "now" [, DateTimeZone $timezone = NULL ]] )
        public DateTime(Context ctx, string time = null, DateTimeZone timezone = null)
            : this(ctx)
        {
            ctx.SetProperty(DateTimeErrors.Empty);
            __construct(time, timezone);
        }

        // public __construct ([ string $time = "now" [, DateTimeZone $timezone = NULL ]] )
        public void __construct(string time = null, DateTimeZone timezone = null)
        {
            if (timezone != null)
            {
                this.TimeZone = timezone._timezone;
            }

            if (this.TimeZone == null)
            {
                //PhpException.InvalidArgument("timezone");
                //return null;
                throw new ArgumentException();
            }

            this.Time = StrToTime(_ctx, time, System_DateTime.UtcNow);

            //this.date.Value = this.Time.ToString("yyyy-mm-dd HH:mm:ss");
            //this.timezone_type.Value = 3;
            //this.timezone.Value = TimeZone.Id;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Parses time and returns <see cref="System_DateTime"/>.
        /// In case error or warning occur, <see cref="DateTimeErrors"/> is set accordingly.
        /// </summary>
        [PhpHidden]
        internal static System_DateTime StrToTime(Context ctx, string timestr, System_DateTime time)
        {
            if (string.IsNullOrWhiteSpace(timestr) || (timestr = timestr.Trim()).EqualsOrdinalIgnoreCase("now"))
            {
                return System_DateTime.UtcNow;
            }

            var result = DateInfo.Parse(ctx, timestr, time, out var error);
            if (error != null)
            {
                ctx.SetProperty<DateTimeErrors>(new DateTimeErrors { Errors = new[] { error } });
                throw new Spl.Exception(error);
            }

            return DateTimeUtils.UnixTimeStampToUtc(result);
        }

        internal DateTimeImmutable AsDateTimeImmutable() => new DateTimeImmutable(_ctx, this.Time, this.TimeZone);

        #endregion

        #region Methods

        public virtual void __wakeup()
        {

        }

        /// <summary>
        /// Returns a new instance of a DateTime object.
        /// </summary>
        /// <param name="array">Initialization array.</param>
        [return: NotNull]
        public static DateTime __set_state(PhpArray array)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Adds an amount of days, months, years, hours, minutes and seconds to a DateTime object.
        /// </summary>
        /// <returns>Returns the <see cref="DateTime"/> object for method chaining or <c>FALSE</c> on failure.</returns>
        //[return: CastToFalse]
        [return: NotNull]
        public virtual DateTime add(DateInterval interval)
        {
            Time = Time.Add(interval.AsTimeSpan());

            return this;
        }

        /// <summary>
        /// Subtracts an amount of days, months, years, hours, minutes and seconds from a DateTime object.
        /// </summary>
        //[return: CastToFalse]
        [return: NotNull]
        public virtual DateTime sub(DateInterval interval)
        {
            Time = Time.Subtract(interval.AsTimeSpan());
            return this;
        }

        /// <summary>
        /// Returns the difference between two DateTime objects
        /// </summary>
        [return: NotNull]
        public virtual DateInterval diff(DateTimeInterface datetime2, bool absolute = false) => DateTimeFunctions.date_diff(this, datetime2, absolute);

        [return: NotNull]
        public virtual DateTime setTimezone(DateTimeZone timezone)
        {
            if (timezone == null)
            {
                //PhpException.ArgumentNull(nameof(timezone));
                //return false;
                throw new ArgumentNullException();
            }

            if (timezone._timezone != this.TimeZone)
            {
                // convert this.Time from old TZ to new TZ
                this.Time = TimeZoneInfo.ConvertTime(new System_DateTime(this.Time.Ticks, DateTimeKind.Unspecified), this.TimeZone, timezone._timezone);
                this.TimeZone = timezone._timezone;
            }

            return this;
        }

        [return: CastToFalse]
        public virtual string format(string format)
        {
            if (format == null)
            {
                //PhpException.ArgumentNull("format");
                //return false;
                throw new ArgumentNullException();
            }

            return DateTimeFunctions.FormatDate(format, this.Time, this.TimeZone);
        }

        public virtual long getOffset()
        {
            if (this.TimeZone == null)
                //return false;
                throw new InvalidOperationException();

            return (long)this.TimeZone.BaseUtcOffset.TotalSeconds;
        }

        [return: NotNull]
        public virtual DateTimeZone getTimezone()
        {
            return new DateTimeZone(this.TimeZone);
        }

        /// <summary>Returns the warnings and errors</summary>
        [return: CastToFalse]
        public static PhpArray getLastErrors(Context ctx) => ctx.TryGetProperty<DateTimeErrors>()?.ToPhpArray();

        [return: NotNull]
        public virtual DateTime modify(string modify)
        {
            if (modify == null)
            {
                //PhpException.ArgumentNull("modify");
                //return false;
                throw new ArgumentNullException();
            }

            this.Time = StrToTime(_ctx, modify, Time);

            return this;
        }

        [return: CastToFalse]
        public static DateTime createFromFormat(Context ctx, string format, string time, DateTimeZone timezone = null)
        {
            // arguments
            var tz = (timezone != null) ? timezone._timezone : PhpTimeZone.GetCurrentTimeZone(ctx);

            var dateinfo = DateInfo.ParseFromFormat(format, time, out var errors);

            ctx.SetProperty<DateTimeErrors>(errors);

            if (errors != null && errors.HasErrors)
            {
                return null;
            }

            return new DateTime(ctx)
            {
                Time = dateinfo.GetDateTime(ctx, System_DateTime.UtcNow),
                TimeZone = tz, // TODO: dateinfo.TimeZone
            };
        }

        [return: NotNull]
        public virtual DateTime setDate(int year, int month, int day)
        {
            try
            {
                var time = TimeZoneInfo.ConvertTime(Time, TimeZone);
                this.Time = TimeZoneInfo.ConvertTime(
                    new System_DateTime(
                        year, month, day,
                        time.Hour, time.Minute, time.Second,
                        time.Millisecond
                    ),
                    TimeZone
                );
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new ArgumentOutOfRangeException(string.Format("The date {0}-{1}-{2} is not valid.", year, month, day), e);
            }

            return this;
        }

        [return: NotNull]
        public virtual DateTime setISODate(int year, int week, int day = 1)
        {
            var jan1 = new System_DateTime(year, 1, 1);
            int daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;

            // Use first Thursday in January to get first week of the year as
            // it will never be in Week 52/53
            var firstThursday = jan1.AddDays(daysOffset);
            var cal = CultureInfo.CurrentCulture.Calendar;
            int firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

            // As we're adding days to a date in Week 1,
            // we need to subtract 1 in order to get the right date for week #1
            if (firstWeek == 1)
            {
                week -= 1;
            }

            // Using the first Thursday as starting week ensures that we are starting in the right year
            // then we add number of weeks multiplied with days
            var result = firstThursday.AddDays(week * 7);

            // Subtract 3 days from Thursday to get Monday, which is the first weekday in ISO8601
            result = result.AddDays(-3);

            // days
            result = result.AddDays(day - 1);

            var time = TimeZoneInfo.ConvertTime(Time, TimeZone);
            this.Time = TimeZoneInfo.ConvertTime(
                new System_DateTime(
                    result.Year, result.Month, result.Day,
                    time.Hour, time.Minute, time.Second,
                    time.Millisecond
                ),
                TimeZone
            );

            return this;
        }

        [return: NotNull]
        public virtual DateTime setTime(int hour, int minute, int second)
        {
            try
            {
                var time = TimeZoneInfo.ConvertTime(Time, TimeZone);
                this.Time = TimeZoneInfo.ConvertTime(
                    new System_DateTime(time.Year, time.Month, time.Day, hour, minute, second),
                    TimeZone
                );
            }
            catch (ArgumentOutOfRangeException e)
            {
                throw new ArgumentOutOfRangeException(string.Format("The time {0}:{1}:{2} is not valid.", hour, minute, second), e);
            }


            return this;
        }

        public virtual long getTimestamp()
        {
            return DateTimeUtils.UtcToUnixTimeStamp(Time);
        }

        /// <summary>
        /// Sets the date and time based on an Unix timestamp.
        /// </summary>
        /// <returns>Returns the <see cref="DateTime"/> object for method chaining.</returns>
        [return: NotNull]
        public DateTime setTimestamp(long unixtimestamp)
        {
            this.Time = DateTimeUtils.UnixTimeStampToUtc(unixtimestamp);
            return this;
        }

        #endregion

        #region IPhpComparable

        int IPhpComparable.Compare(PhpValue obj) => DateTimeFunctions.CompareTime(this.Time, obj);

        #endregion

        #region IPhpCloneable

        [PhpHidden]
        public object Clone()
        {
            if (GetType() == typeof(DateTime))
            {
                return new DateTime(_ctx, Time, TimeZone);
            }
            else
            {
                return Operators.CloneRaw(_ctx, this);
            }
        }

        #endregion

        #region CLR Conversions

        /// <summary>
        /// Gets given PHP DateTime as <see cref="System_DateTime"/> (UTC).
        /// </summary>
        public static implicit operator System_DateTime(DateTime dt) => TimeZoneInfo.ConvertTime(dt.Time, dt.TimeZone);

        #endregion
    }

    /// <summary>
    /// Representation of date and time.
    /// </summary>
    [PhpType(PhpTypeAttribute.InheritName)]
    public class DateTimeImmutable : DateTimeInterface, IPhpComparable, IPhpCloneable
    {
        readonly protected Context _ctx;

        /// <summary>
        /// Get the date-time value, stored in UTC
        /// </summary>
        internal System_DateTime Time { get; private set; }

        /// <summary>
        /// Get the time zone for this DateTime object
        /// </summary>
        internal TimeZoneInfo TimeZone { get; private set; }

        internal DateTimeImmutable(Context ctx, System_DateTime time, TimeZoneInfo tz)
        {
            Debug.Assert(ctx != null);

            this._ctx = ctx;
            this.Time = time;
            this.TimeZone = tz;
        }

        // public __construct ([ string $time = "now" [, DateTimeZone $timezone = NULL ]] )
        public DateTimeImmutable(Context ctx, string time = null, DateTimeZone timezone = null)
        {
            Debug.Assert(ctx != null);

            ctx.SetProperty(DateTimeErrors.Empty);

            this.TimeZone = (timezone == null)
                ? PhpTimeZone.GetCurrentTimeZone(ctx)
                : timezone._timezone;

            if (TimeZone == null)
            {
                //PhpException.InvalidArgument("timezone");
                //return null;
                throw new ArgumentException();
            }

            this._ctx = ctx;
            this.Time = DateTime.StrToTime(ctx, time, System_DateTime.UtcNow);

            //this.date.Value = this.Time.ToString("yyyy-mm-dd HH:mm:ss");
            //this.timezone_type.Value = 3;
            //this.timezone.Value = TimeZone.Id;
        }

        #region DateTimeInterface

        [return: CastToFalse]
        public string format(string format)
        {
            if (format == null)
            {
                //PhpException.ArgumentNull("format");
                //return false;
                throw new ArgumentNullException();
            }

            return DateTimeFunctions.FormatDate(format, this.Time, this.TimeZone);
        }

        public long getOffset()
        {
            if (this.TimeZone == null)
                //return false;
                throw new InvalidOperationException();

            return (long)this.TimeZone.BaseUtcOffset.TotalSeconds;
        }

        public long getTimestamp()
        {
            return DateTimeUtils.UtcToUnixTimeStamp(Time);
        }

        public DateTimeImmutable setTimestamp(int unixtimestamp)
        {
            return new DateTimeImmutable(_ctx, DateTimeUtils.UnixTimeStampToUtc(unixtimestamp), this.TimeZone);
        }

        public DateTimeZone getTimezone()
        {
            return new DateTimeZone(this.TimeZone);
        }

        public virtual DateTimeImmutable modify(string modify)
        {
            if (modify == null)
            {
                //PhpException.ArgumentNull("modify");
                //return false;
                throw new ArgumentNullException();
            }

            return new DateTimeImmutable(_ctx, DateTime.StrToTime(_ctx, modify, Time), this.TimeZone);
        }

        public void __wakeup() { }

        #endregion

        public virtual DateTimeImmutable add(DateInterval interval) => new DateTimeImmutable(_ctx, Time.Add(interval.AsTimeSpan()), TimeZone);
        [return: CastToFalse]
        public static DateTimeImmutable createFromFormat(Context ctx, string format, string time, DateTimeZone timezone = null)
        {
            // arguments
            var tz = (timezone != null) ? timezone._timezone : PhpTimeZone.GetCurrentTimeZone(ctx);

            var dateinfo = DateInfo.ParseFromFormat(format, time, out var errors);

            ctx.SetProperty<DateTimeErrors>(errors);

            if (errors != null && errors.HasErrors)
            {
                return null;
            }

            return new DateTimeImmutable(ctx, dateinfo.GetDateTime(ctx, System_DateTime.UtcNow), tz); // TODO: dateinfo.TimeZones
        }

        public static DateTimeImmutable createFromMutable(DateTime datetime)
        {
            if (datetime == null)
            {
                PhpException.ArgumentNull(nameof(datetime));
                return null;
            }
            else
            {
                return datetime.AsDateTimeImmutable();
            }
        }

        [return: CastToFalse]
        public static PhpArray getLastErrors(Context ctx) => DateTime.getLastErrors(ctx);
        public static DateTimeImmutable __set_state(PhpArray array) => throw new NotImplementedException();
        public virtual DateTimeImmutable setDate(int year, int month, int day) => throw new NotImplementedException();
        public virtual DateTimeImmutable setISODate(int year, int week, int day = 1) => throw new NotImplementedException();
        public virtual DateTimeImmutable setTime(int hour, int minute, int second = 0, int microseconds = 0) => throw new NotImplementedException();

        [return: NotNull]
        public virtual DateTimeImmutable setTimezone(DateTimeZone timezone)
        {
            if (timezone == null)
            {
                PhpException.ArgumentNull(nameof(timezone));
                return this;
            }

            if (timezone._timezone == this.TimeZone)
            {
                return this;
            }
            else
            {
                // convert this.Time from old TZ to new TZ
                var time = TimeZoneInfo.ConvertTime(new System_DateTime(this.Time.Ticks, DateTimeKind.Unspecified), this.TimeZone, timezone._timezone);

                return new DateTimeImmutable(_ctx, time, timezone._timezone);
            }
        }
        public virtual DateTimeImmutable sub(DateInterval interval) => new DateTimeImmutable(_ctx, Time.Subtract(interval.AsTimeSpan()), TimeZone);

        /// <summary>
        /// Returns the difference between two DateTime objects
        /// </summary>
        public virtual DateInterval diff(DateTimeInterface datetime2, bool absolute = false) => DateTimeFunctions.date_diff(this, datetime2, absolute);

        #region IPhpComparable

        int IPhpComparable.Compare(PhpValue obj) => DateTimeFunctions.CompareTime(this.Time, obj);

        #endregion

        #region IPhpCloneable

        public object Clone() => new DateTimeImmutable(_ctx, Time, TimeZone);

        #endregion
    }
}
