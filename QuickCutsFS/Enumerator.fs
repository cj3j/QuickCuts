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

open System.Collections.Generic

module Enumerator =

    type Enumerator<'T> = {
        current: 'T option;
        ienum: IEnumerator<'T>;
    }
    with
        static member next (enum:Enumerator<'T>) =
            match enum.current with
            | Some value ->
                match enum.ienum.MoveNext() with
                | true -> { enum with current = Some enum.ienum.Current }
                | false -> { enum with current = None }
            | None -> enum

        static member make (ienum:IEnumerator<'T>) =
            match ienum.MoveNext() with
                | true -> { current = Some ienum.Current; ienum = ienum }
                | false -> { current = None; ienum = ienum }