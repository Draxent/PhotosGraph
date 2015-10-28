namespace IUM.MidTerm

open System.Drawing

type Vector() =
  // Define
  let pi = float32 System.Math.PI

  // Property
  let mutable modulev = 0.f
  let mutable angle = 0.f // expressed in radians

  // Module
  member this.Module
    with get() = modulev
    and set(v) = modulev <- abs(v)

  member this.X
    with get() = modulev * cos(angle)

  member this.Y
    with get() = modulev * sin(angle)

  // Angle
  member this.Angle
    with get() = angle
    and set(v) = angle <- v

  static member RadiansToDegrees(v:float32) :
    float32 =  v * (180.f / float32 System.Math.PI)

  static member DegreesToRadians(v:float32) :
    float32 =  v * (float32 System.Math.PI / 180.f)

  // Distance between two points
  static member Distance(p1:PointF, p2:PointF) :
    float32 = sqrt((p1.X - p2.X) ** 2.f + (p1.Y - p2.Y) ** 2.f)

  // Calculate the angular coefficient between $p1 and $p2 in radians
  static member Angular_Coefficient(p1:PointF, p2:PointF) :
    float32 =
      let (vx, vy) = (p2.X - p1.X, p2.Y - p1.Y)
      if (vx = 0.f && vy = 0.f) then 0.f
      else atan(vy / vx)

  static member Direction(c:PointF, p:PointF, v1, v2, ?neq:bool) :
    'a =
      let cond = if neq.IsSome && neq.Value then (p.X - c.X > 0.f) else (p.X - c.X >= 0.f)
      if cond then v1 else v2

  static member TrasformInVector(vx:float32, vy:float32) :
    Vector =
      let pi = float32 System.Math.PI
      let modulex = sqrt(vx**(2.f) + vy**(2.f))
      let anglex =
        if (vy = 0.f && vx = 0.f) then 0.f
        elif vx < 0.f then atan(vy / vx) + pi
        else atan(vy / vx)
      new Vector(Module = modulex, Angle = anglex)      

  static member (~-) (v:Vector) :
    Vector =
      let pi = float32 System.Math.PI
      new Vector(Module = v.Module, Angle = v.Angle + pi)

  static member (+) (v1:Vector, v2:Vector) :
    Vector =
      let pi = float32 System.Math.PI
      let (vx, vy) = (v1.X + v2.X, v1.Y + v2.Y)
      let modulex = sqrt(vx**(2.f) + vy**(2.f))
      let anglex =
        if (vy = 0.f && vx = 0.f) then 0.f
        elif vx < 0.f then atan(vy / vx) + pi
        else atan(vy / vx)
      new Vector(Module = modulex, Angle = anglex)

  static member (-) (v1:Vector, v2:Vector) :
    Vector =
      let pi = float32 System.Math.PI
      let (vx, vy) = (v1.X - v2.X, v1.Y - v2.Y)
      let modulex = sqrt(vx**(2.f) + vy**(2.f))
      let anglex =
        if (vy = 0.f && vx = 0.f) then 0.f
        elif vx < 0.f then atan(vy / vx) + pi
        else atan(vy / vx)
      new Vector(Module = modulex, Angle = anglex)

  static member (*) (v:Vector, s:float32) :
    Vector =
      let pi = float32 System.Math.PI
      let plus_angle = if s < 0.f then pi else 0.f
      new Vector(Module = abs(s)*v.Module, Angle = v.Angle + plus_angle)

  static member (*) (s:float32, v:Vector) :
    Vector =
      let pi = float32 System.Math.PI
      let plus_angle = if s < 0.f then pi else 0.f
      new Vector(Module = abs(s)*v.Module, Angle = v.Angle + plus_angle)

  static member (/) (v:Vector, s:float32) :
    Vector =
      let pi = float32 System.Math.PI
      let plus_angle = if s < 0.f then pi else 0.f
      new Vector(Module = v.Module / abs(s), Angle = v.Angle + plus_angle)