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

open Result

module Macros =

    module Errors =
        type MacroNotFound = { key:string }

        type Error =
        | MacroNotFound of MacroNotFound

    module Exceptions = 
        type MacroNotFound(key:string) =
            inherit System.Exception(sprintf "Macro '%s' not found" key)
            let key = key

    type StringMap = (string -> string option)

    let rec getValue (key:string) (map:StringMap) =
        let valueOption = map key
        match valueOption with
        | None -> None
        | Some value -> Some <| expandValue value map

    and expandValue (value:string) (map:StringMap) =
        let rec replaceKeys charStart charEnd getValueFunc text =
            let keyOption = getNextKey charStart charEnd text
            match keyOption with
            | None -> text
            | Some key ->
                let replacement = getValueFunc key
                let replaceKey = charStart + key + charEnd
                let expandedValue = String.replace replaceKey replacement text
                replaceKeys charStart charEnd getValueFunc expandedValue

        and getNextKey charStart charEnd text =
            let startIdxOption = String.indexOf charStart text
            match startIdxOption with
            | None -> None
            | Some startIdx ->
                let endIdxOption = String.indexOfAt charEnd (startIdx + 1) text
                match endIdxOption with
                | None -> None
                | Some endIdx ->
                    let key = String.subStringMid (startIdx+1) (endIdx - startIdx - 1) text
                    Some key

        let getValueRequired = fun map key ->
            match map key with
            | Some v -> v
            | None -> raise <| Exceptions.MacroNotFound(key)

        let getValueOptional = fun map key ->
            match map key with
            | Some v -> v
            | None -> ""

        let rq = replaceKeys "{" "}" (getValueRequired map) value
        let ro = rq |> replaceKeys "[" "]" (getValueOptional map)
        ro
