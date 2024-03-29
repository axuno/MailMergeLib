﻿using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;

namespace MailMergeLib.Tests;

internal class Helper
{
    /// <summary>
    /// Gets the path of code base for the executing assembly.
    /// </summary>
    /// <remarks>
    /// The Assembly.Location property sometimes gives wrong results when using NUnit (where assemblies run from a temporary folder).
    /// That's why we need reliable way to find the assembly location, which is the base for relativ data folders.
    /// </remarks>
    /// <returns></returns>
    public static string GetCodeBaseDirectory()
    {
        return Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().Location).LocalPath)!;
    }

    internal static int Compare(Stream? a, Stream? b)
    {
        if (a == null && b == null) return 0;

        if (a == null || b == null) throw new ArgumentNullException(a == null ? "a" : "b");

        a.Position = b.Position = 0;

        if (a.Length < b.Length) return -1;

        if (a.Length > b.Length) return 1;

        int bufa;
        while ((bufa = a.ReadByte()) != -1)
        {
            var bufb = b.ReadByte();
            var diff = bufa.CompareTo(bufb);
            if (diff != 0) return diff;
        }
        return 0;
    }

    /// <summary>
    /// The method will select the first free port after and
    /// including the given <see paramref="startPort"/>.
    /// </summary>
    /// <returns>The first free TCP port found.</returns>
    /// <exception cref="InvalidCastException">If no free port could be found.</exception>

    internal static int GetFreeTcpPort(int startPort = 2000) 
    {
        for (var i = startPort; i <= 0xFFFF; i++)
        {
            if (IsFreePort(i) && CanBindPort(i)) return i;
        }

        throw new InvalidOperationException("No free TCP port found");
    }

    private static bool IsFreePort(int port)
    {
        var properties = IPGlobalProperties.GetIPGlobalProperties();
        var listeners = properties.GetActiveTcpListeners();
        var openPorts = listeners.Select(item => item.Port).ToArray<int>();
        return openPorts.All(openPort => openPort != port);
    }

    private static bool CanBindPort(int port)
    {
        try
        {
            var localEndPoint = new IPEndPoint(IPAddress.Any, port);
            using var listener = new Socket(IPAddress.Any.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(localEndPoint);
        }
        catch
        {
            // e.g. because of "Permission denied" or other reason
            return false;
        }

        return true;
    }
}