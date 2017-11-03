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

module Result =

    type Result<'T, 'E> = 
        | Success of 'T
        | Failure of 'E

    let inline bind input func =
        match input with
        | Success v -> func v
        | Failure f -> Failure f

    let (>>=) = bind

    let map f r =
        match r with
        | Success v -> Success v
        | Failure err -> Failure (f err)

    let ifSuccess success failure result =
        match result with
        | Success v -> success v
        | Failure f -> failure f

    type ResultExprBuilder() =
        member this.Bind(m, f) = bind m f
        member this.Return(x) = Success x
        member this.ReturnFrom(x) = x

    let ResultExpr = new ResultExprBuilder()