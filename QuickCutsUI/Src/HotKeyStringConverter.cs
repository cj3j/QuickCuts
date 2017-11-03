// QuickCuts Copyright (c) 2017 C. Jared Cone jared.cone@gmail.com
//
// This software is provided 'as-is', without any express or implied
// warranty.  In no event will the authors be held liable for any damages
// arising from the use of this software.
// Permission is granted to anyone to use this software for any purpose,
// including commercial applications, and to alter it and redistribute it
// freely, subject to the following restrictions:
// 1. The origin of this software must not be misrepresented; you must not
//    claim that you wrote the original software. If you use this software
//    in a product, an acknowledgment in the product documentation would be
//    appreciated but is not required.
// 2. Altered source versions must be plainly marked as such, and must not be
//    misrepresented as being the original software.
// 3. This notice may not be removed or altered from any source distribution.

using System;
using System.Collections.Generic;
using System.Windows.Input;

namespace QuickCutsUI
{
	public static class HotKeyStringConverter
    {
        static Dictionary<Key, ModifierKeys> _keyToModifierMap;
        const string _delimiter = " + ";

        static HotKeyStringConverter()
        {
            _keyToModifierMap = new Dictionary<Key, ModifierKeys>();

            _keyToModifierMap.Add(Key.LeftAlt, ModifierKeys.Alt);
            _keyToModifierMap.Add(Key.RightAlt, ModifierKeys.Alt);

            _keyToModifierMap.Add(Key.LeftCtrl, ModifierKeys.Control);
            _keyToModifierMap.Add(Key.RightCtrl, ModifierKeys.Control);

            _keyToModifierMap.Add(Key.LeftShift, ModifierKeys.Shift);
            _keyToModifierMap.Add(Key.RightShift, ModifierKeys.Shift);

            _keyToModifierMap.Add(Key.LWin, ModifierKeys.Windows);
            _keyToModifierMap.Add(Key.RWin, ModifierKeys.Windows);
        }

        /**
         * If any of the two keys represent a modifier key, return the value of the modifier key.
         * Otherwise return ModifierKeys.None
         */
        public static ModifierKeys GetModifierKey(Key sysKey, Key regKey)
        {
            ModifierKeys modKey;

            if (_keyToModifierMap.TryGetValue(sysKey, out modKey) ||
				_keyToModifierMap.TryGetValue(regKey, out modKey))
            {
                return modKey;
            }

            return ModifierKeys.None;
        }

        /**
         * Convert a hotkey pair into a string format, which can be deconverted back into keys later
         */
        public static string ToString(ModifierKeys modKey, Key regKey)
        {
            if (modKey != ModifierKeys.None && regKey != Key.None)
            {
                return new ModifierKeysConverter().ConvertToString(modKey) + _delimiter + new KeyConverter().ConvertToString(regKey);
            }

			if (modKey != ModifierKeys.None)
            {
                return new ModifierKeysConverter().ConvertToString(modKey);
            }

			if (regKey != Key.None)
            {
                return new KeyConverter().ConvertToString(regKey);
            }

			return "";
		}

        /**
         * Convert a hotkey string back into a modifier key and regular key.
         * Return true if successful
         */
        public static bool FromString(string str, out ModifierKeys modKey, out Key regKey)
        {
            modKey = ModifierKeys.None;
            regKey = Key.None;

            if (String.IsNullOrEmpty(str))
            {
                return false;
            }

            int index = str.IndexOf(_delimiter);

            if (index > 0)
            {
                string modStr = str.Substring(0, index);
                string regStr = str.Substring(index + _delimiter.Length);

                modKey = (ModifierKeys)new ModifierKeysConverter().ConvertFromString(modStr);
                regKey = (Key)new KeyConverter().ConvertFromString(regStr);
                return true;
            }

            modKey = (ModifierKeys)new ModifierKeysConverter().ConvertFromString(str);

            if (modKey != ModifierKeys.None)
            {
                return true;
            }

            regKey = (Key)new KeyConverter().ConvertFromString(str);

            if (regKey != Key.None)
            {
                return true;
            }

            return false;
        }
    }
}
