using System;
using System.Runtime.InteropServices;
using Dawn;
using static Dawn.Guard;

namespace Quelimb
{
    [StructLayout(LayoutKind.Auto)]
    public readonly struct StringOrFormattableString
    {
        public string String { get; }

        public FormattableString FormattableString { get; }

        public bool IsFormattable => this.FormattableString != null;

        public bool IsDefault => this.String == null && this.FormattableString == null;

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

        public override string ToString()
        {
            return this.String ?? this.FormattableString?.ToString() ?? "";
        }
    }

    internal static class StringOrFormattableStringGuard
    {
        public static ref readonly ArgumentInfo<StringOrFormattableString> NotDefault(in this ArgumentInfo<StringOrFormattableString> argument)
        {
            if (argument.Value.IsDefault)
            {
                throw Fail(new ArgumentException(argument.Name + " cannot be default.", argument.Name));
            }

            return ref argument;
        }

        public static ref readonly ArgumentInfo<StringOrFormattableString> NotEmpty(in this ArgumentInfo<StringOrFormattableString> argument)
        {
            argument.Wrap(x => x.IsFormattable ? x.FormattableString.Format : x.String).NotEmpty();
            return ref argument;
        }
    }
}
