using System;
using System.Runtime.InteropServices;
using static Dawn.Guard;

namespace Quelimb
{
    [StructLayout(LayoutKind.Auto)]
    internal readonly struct StringOrFormattableString
    {
        public string? String { get; }

        public FormattableString? FormattableString { get; }

        public readonly bool IsFormattable => this.FormattableString != null;

        public readonly bool IsDefault => this.String == null && this.FormattableString == null;

        public StringOrFormattableString(string s)
        {
            this.String = s;
            this.FormattableString = null;
        }

        public StringOrFormattableString(FormattableString s)
        {
            this.String = null;
            this.FormattableString = s;
        }

        public override readonly string ToString()
        {
            return this.String ?? this.FormattableString?.ToString() ?? "";
        }
    }

    internal static class StringOrFormattableStringGuard
    {
        public static ref readonly ArgumentInfo<StringOrFormattableString> NotDefault(in this ArgumentInfo<StringOrFormattableString> argument)
        {
            if (argument.Value.IsDefault)
                throw Fail(new ArgumentException(argument.Name + " cannot be default.", argument.Name));
            return ref argument;
        }

        public static ref readonly ArgumentInfo<StringOrFormattableString> NotEmpty(in this ArgumentInfo<StringOrFormattableString> argument)
        {
            argument.Wrap(x => x.IsFormattable ? x.FormattableString!.Format : x.String).NotEmpty();
            return ref argument;
        }

        public static ref readonly ArgumentInfo<FormattableString> NotEmpty(in this ArgumentInfo<FormattableString> argument)
        {
            if (string.IsNullOrEmpty(argument.Value.Format))
                throw Fail(new ArgumentException(argument.Name + " cannot be empty.", argument.Name));
            return ref argument;
        }
    }
}
