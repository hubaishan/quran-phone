﻿using Microsoft.Phone.Net.NetworkInformation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace QuranPhone.Utils
{
    public class QuranUtils
    {

        public static bool DoesStringContainArabic(String s)
        {
            if (s == null) return false;

            int length = s.Length;
            for (int i = 0; i < length; i++)
            {
                int current = (int)s[i];
                // Skip space
                if (current == 32)
                    continue;
                // non-reshaped arabic
                if ((current >= 1570) && (current <= 1610))
                    return true;
                // re-shaped arabic
                else if ((current >= 65133) && (current <= 65276))
                    return true;
                // if the value is 42, it deserves another chance :p
                // (in reality, 42 is a * which is useful in searching sqlite)
                else if (current != 42)
                    return false;
            }
            return false;
        }

        public static bool IsOnWifiNetwork()
        {
            return NetworkInterface.GetIsNetworkAvailable() && NetworkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211;
        }
    }
}