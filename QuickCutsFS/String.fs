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

namespace QuickCutsFS

module String =

    let trimStart (str:string) = str.TrimStart()
    let trimEnd (str:string) = str.TrimStart()
    let trim (str:string) = str.Trim()
    let trimChars (str:string) (chr:char) = str.Trim(chr)
    let indexOfAt (subStr:string) (startIndex:int) (str:string) =
        let idx = str.IndexOf(subStr, startIndex)
        if idx >= 0 then Some idx else None
    let indexOf (subStr:string) (str:string) = indexOfAt subStr 0 str
    let subStringStart (length:int) (str:string) = str.Substring(0, length)
    let subStringEnd (start:int) (str:string) = str.Substring(start)
    let subStringMid (start:int) (length:int) (str:string) = str.Substring(start, length)
    let replace (find:string) (replace:string) (target:string) = target.Replace(find, replace)
    let hashcode (str:string) = str.GetHashCode()
    let join (strs:string list) (separator:string) = System.String.Join(separator, strs)

    let countChars predicate line =
        let rec countCharsAt index count =
            if index < String.length line then
                if predicate line.[index] then
                    countCharsAt (index + 1) (count + 1)
                else
                    count
            else
                count
        countCharsAt 0 0

    