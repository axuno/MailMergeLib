﻿//
// EmailValidator.cs
//
// Author: Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2013-2019 Jeffrey Stedfast
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;

namespace MailMergeLib;

/// <summary>
/// An Email validator.
/// </summary>
/// <remarks>
/// An Email validator.
/// </remarks>
public static class EmailValidator
{
    const string AtomCharacters = "!#$%&'*+-/=?^_`{|}~";

    [Flags]
    enum SubDomainType
    {
        None = 0,
        Alphabetic = 1,
        Numeric = 2,
        AlphaNumeric = 3
    }

    static bool IsDigit(char c)
    {
        return (c >= '0' && c <= '9');
    }

    static bool IsLetter(char c)
    {
        return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
    }

    static bool IsLetterOrDigit(char c)
    {
        return IsLetter(c) || IsDigit(c);
    }

    static bool IsAtom(char c, bool allowInternational)
    {
        return c < 128 ? IsLetterOrDigit(c) || AtomCharacters.IndexOf(c) != -1 : allowInternational;
    }

    static bool IsDomain(char c, bool allowInternational, ref SubDomainType type)
    {
        if (c < 128)
        {
            if (IsLetter(c) || c == '-')
            {
                type |= SubDomainType.Alphabetic;
                return true;
            }

            if (IsDigit(c))
            {
                type |= SubDomainType.Numeric;
                return true;
            }

            return false;
        }

        if (allowInternational)
        {
            type |= SubDomainType.Alphabetic;
            return true;
        }

        return false;
    }

    static bool IsDomainStart(char c, bool allowInternational, out SubDomainType type)
    {
        if (c < 128)
        {
            if (IsLetter(c))
            {
                type = SubDomainType.Alphabetic;
                return true;
            }

            if (IsDigit(c))
            {
                type = SubDomainType.Numeric;
                return true;
            }

            type = SubDomainType.None;

            return false;
        }

        if (allowInternational)
        {
            type = SubDomainType.Alphabetic;
            return true;
        }

        type = SubDomainType.None;

        return false;
    }

    static bool SkipAtom(string text, ref int index, bool allowInternational)
    {
        var startIndex = index;

        while (index < text.Length && IsAtom(text[index], allowInternational))
            index++;

        return index > startIndex;
    }

    static bool SkipSubDomain(string text, ref int index, bool allowInternational, out SubDomainType type)
    {
        var startIndex = index;

        if (!IsDomainStart(text[index], allowInternational, out type))
            return false;

        index++;

        while (index < text.Length && IsDomain(text[index], allowInternational, ref type))
            index++;

        return (index - startIndex) < 64 && text[index - 1] != '-';
    }

    static bool SkipDomain(string text, ref int index, bool allowTopLevelDomains, bool allowInternational)
    {

        if (!SkipSubDomain(text, ref index, allowInternational, out var type))
            return false;

        if (index < text.Length && text[index] == '.')
        {
            do
            {
                index++;

                if (index == text.Length)
                    return false;

                if (!SkipSubDomain(text, ref index, allowInternational, out type))
                    return false;
            } while (index < text.Length && text[index] == '.');
        }
        else if (!allowTopLevelDomains)
        {
            return false;
        }

        // Note: by allowing AlphaNumeric, we get away with not having to support punycode.
        if (type == SubDomainType.Numeric)
            return false;

        return true;
    }

    static bool SkipQuoted(string text, ref int index, bool allowInternational)
    {
        var escaped = false;

        // skip over leading '"'
        index++;

        while (index < text.Length)
        {
            if (text[index] >= 128 && !allowInternational)
                return false;

            if (text[index] == '\\')
            {
                escaped = !escaped;
            }
            else if (!escaped)
            {
                if (text[index] == '"')
                    break;
            }
            else
            {
                escaped = false;
            }

            index++;
        }

        if (index >= text.Length || text[index] != '"')
            return false;

        index++;

        return true;
    }

    static bool SkipIPv4Literal(string text, ref int index)
    {
        var groups = 0;

        while (index < text.Length && groups < 4)
        {
            var startIndex = index;
            var value = 0;

            while (index < text.Length && text[index] >= '0' && text[index] <= '9')
            {
                value = (value * 10) + (text[index] - '0');
                index++;
            }

            if (index == startIndex || index - startIndex > 3 || value > 255)
                return false;

            groups++;

            if (groups < 4 && index < text.Length && text[index] == '.')
                index++;
        }

        return groups == 4;
    }

    static bool IsHexDigit(char c)
    {
        return (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f') || (c >= '0' && c <= '9');
    }

    // This needs to handle the following forms:
    //
    // IPv6-addr = IPv6-full / IPv6-comp / IPv6v4-full / IPv6v4-comp
    // IPv6-hex  = 1*4HEXDIG
    // IPv6-full = IPv6-hex 7(":" IPv6-hex)
    // IPv6-comp = [IPv6-hex *5(":" IPv6-hex)] "::" [IPv6-hex *5(":" IPv6-hex)]
    //             ; The "::" represents at least 2 16-bit groups of zeros
    //             ; No more than 6 groups in addition to the "::" may be
    //             ; present
    // IPv6v4-full = IPv6-hex 5(":" IPv6-hex) ":" IPv4-address-literal
    // IPv6v4-comp = [IPv6-hex *3(":" IPv6-hex)] "::"
    //               [IPv6-hex *3(":" IPv6-hex) ":"] IPv4-address-literal
    //             ; The "::" represents at least 2 16-bit groups of zeros
    //             ; No more than 4 groups in addition to the "::" and
    //             ; IPv4-address-literal may be present
    static bool SkipIPv6Literal(string text, ref int index)
    {
        var compact = false;
        var colons = 0;

        while (index < text.Length)
        {
            var startIndex = index;

            while (index < text.Length && IsHexDigit(text[index]))
                index++;

            if (index >= text.Length)
                break;

            if (index > startIndex && colons > 2 && text[index] == '.')
            {
                // IPv6v4
                index = startIndex;

                if (!SkipIPv4Literal(text, ref index))
                    return false;

                return compact ? colons < 6 : colons == 6;
            }

            var count = index - startIndex;
            if (count > 4)
                return false;

            if (text[index] != ':')
                break;

            startIndex = index;
            while (index < text.Length && text[index] == ':')
                index++;

            count = index - startIndex;
            if (count > 2)
                return false;

            if (count == 2)
            {
                if (compact)
                    return false;

                compact = true;
                colons += 2;
            }
            else
            {
                colons++;
            }
        }

        if (colons < 2)
            return false;

        return compact ? colons < 7 : colons == 7;
    }

    /// <summary>
    /// Validate the specified email address.
    /// </summary>
    /// <remarks>
    /// <para>Validates the syntax of an email address.</para>
    /// <para>If <paramref name="allowTopLevelDomains"/> is <c>true</c>, then the validator will
    /// allow addresses with top-level domains like <c>postmaster@dk</c>.</para>
    /// <para>If <paramref name="allowInternational"/> is <c>true</c>, then the validator
    /// will use the newer International Email standards for validating the email address.</para>
    /// </remarks>
    /// <returns><c>true</c> if the email address is valid; otherwise, <c>false</c>.</returns>
    /// <param name="email">An email address.</param>
    /// <param name="allowTopLevelDomains"><c>true</c> if the validator should allow addresses at top-level domains; otherwise, <c>false</c>.</param>
    /// <param name="allowInternational"><c>true</c> if the validator should allow international characters; otherwise, <c>false</c>.</param>
    /// <exception cref="System.ArgumentNullException">
    /// <paramref name="email"/> is <c>null</c>.
    /// </exception>
    public static bool Validate(string email, bool allowTopLevelDomains = false, bool allowInternational = false)
    {
        var index = 0;

        if (email == null)
            throw new ArgumentNullException(nameof(email));

        if (email.Length == 0 || email.Length >= 255)
            return false;

        // Local-part = Dot-string / Quoted-string
        //       ; MAY be case-sensitive
        //
        // Dot-string = Atom *("." Atom)
        //
        // Quoted-string = DQUOTE *qcontent DQUOTE
        if (email[index] == '"')
        {
            if (!SkipQuoted(email, ref index, allowInternational) || index >= email.Length)
                return false;
        }
        else
        {
            if (!SkipAtom(email, ref index, allowInternational) || index >= email.Length)
                return false;

            while (email[index] == '.')
            {
                index++;

                if (index >= email.Length)
                    return false;

                if (!SkipAtom(email, ref index, allowInternational))
                    return false;

                if (index >= email.Length)
                    return false;
            }
        }

        if (index + 1 >= email.Length || index > 64 || email[index++] != '@')
            return false;

        if (email[index] != '[')
        {
            // domain
            if (!SkipDomain(email, ref index, allowTopLevelDomains, allowInternational))
                return false;

            return index == email.Length;
        }

        // address literal
        index++;

        // we need at least 8 more characters
        if (index + 8 >= email.Length)
            return false;

        var ipv6 = email.Substring(index, 5);
        if (ipv6.ToLowerInvariant() == "ipv6:")
        {
            index += "IPv6:".Length;
            if (!SkipIPv6Literal(email, ref index))
                return false;
        }
        else
        {
            if (!SkipIPv4Literal(email, ref index))
                return false;
        }

        if (index >= email.Length || email[index++] != ']')
            return false;

        return index == email.Length;
    }
}
