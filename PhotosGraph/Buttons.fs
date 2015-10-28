namespace IUM.MidTerm

open System.Drawing
open System.Drawing.Drawing2D

//////////////////////////////
//////////  BUTTON  //////////
//////////////////////////////
[<AbstractClass>]
type Button(x, y, w, h) =
  member this.Position = new PointF(x, y)
  member this.Size = new SizeF(w, h)
  member this.Rectangle = new RectangleF(x, y, w, h)

  member this.Paint (g:Graphics) =
    let gs = g.Save()
    g.TranslateTransform(float32 x, float32 y)
    this.OnPaint(g)
    g.Restore(gs)

  abstract OnPaint : Graphics -> unit
  abstract Contains : (PointF) -> bool

////////////////////////////////////
//////////  SCROLLBUTTON  //////////
////////////////////////////////////
type scrollDir = Up | Down | Left | Right
type ScrollButton(dir:scrollDir, x, y, w, h) =
  inherit Button(x, y, w, h)
  
  // Vars
  let mutable points  = [| new PointF() |]
  let realpath        = new GraphicsPath()

  // Constructor
  do
    match dir with
      | Right ->  points <- [| new PointF(0.f, 0.f);  new PointF(0.f, h); new PointF(w, h / 2.f)    |]
      | Up ->     points <- [| new PointF(0.f, h);    new PointF(w, h);   new PointF(w / 2.f, 0.f)  |]
      | Left ->   points <- [| new PointF(w, 0.f);    new PointF(w, h);   new PointF(0.f, h / 2.f)  |]
      | Down ->   points <- [| new PointF(0.f, 0.f);  new PointF(w, 0.f); new PointF(w / 2.f, h)    |]
    // Calculate the path with the real coordinates
    realpath.AddPolygon(points)
    let m = new Matrix()
    m.Translate(x, y)
    realpath.Transform(m)

  override this.OnPaint g =
    // Graphical elements

    g.FillPolygon(Brushes.Black, points)

  member this.Dir = dir

  static member TakeDir(dir) :
    PointF =
      match dir with
      | Right ->  new PointF(-10.f, 0.f)
      | Up ->     new PointF(0.f, 10.f)
      | Left ->   new PointF(10.f, 0.f)
      | Down ->   new PointF(0.f, -10.f)

  override this.Contains p =
    realpath.IsVisible(p)

//////////////////////////////////
//////////  ZOOMBUTTON  //////////
//////////////////////////////////
type zoomDir = ZoomIn | ZoomOut
type ZoomButton(dir:zoomDir, x, y, w, h) =
  inherit Button(x, y, w, h)

  let distance(p1:PointF, p2:PointF) =
    sqrt((p1.X - p2.X) ** 2.f + (p1.Y - p2.Y) ** 2.f)

  // Center
  member this.Center
    with get() = new PointF(x + w/2.f, y + h/2.f)

  // Radius
  member this.Radius
    with get() = (max w h)/2.f

  override this.OnPaint g =
    // Graphical elements
    use draw_pen = new Pen(Color.White, 2.f)

    g.FillEllipse(Brushes.Black, new RectangleF(0.f, 0.f, w, h))
    match dir with
    | ZoomIn ->
      g.DrawLine(draw_pen, w/6.f, h/2.f, w - w/6.f, h/2.f)
      g.DrawLine(draw_pen, w/2.f, h/6.f, w/2.f, h - h/6.f)
    | ZoomOut ->
      g.DrawLine(draw_pen, w/6.f, h/2.f, w - w/6.f, h/2.f)
   
  member this.Dir = dir

  static member TakeDir(dir) :
    float32 =
      let scale_factor = 1.1f
      match dir with
        | ZoomIn ->   scale_factor
        | ZoomOut ->  1.f / scale_factor

  override this.Contains p =
    (distance(this.Center, p) <= this.Radius)

////////////////////////////////////
//////////  ROTATEBUTTON  //////////
////////////////////////////////////
type rotateDir = Clockwise | Anticlockwise
type RotateButton(dir:rotateDir, x, y, w, h) =
  inherit Button(x, y, w, h)

  // Function to mirrow respect to $valx all the points $pts
  let mirroring(pts:PointF[], valx:float32) =
    let mutable newpts = pts.Clone() :?> PointF []
    for i in 0 .. (newpts.Length - 1) do
      let h = valx - newpts.[i].X
      newpts.[i] <- new PointF(newpts.[i].X + 2.f*h - valx, newpts.[i].Y)
    newpts

  // Define
  let (px, py) = (w / 60.f, h / 50.f) // pixel
  let mutable arrow_clockwise = [|
    new PointF(41.f*px, 34.f*py)
    new PointF(31.f*px, 34.f*py)
    new PointF(45.f*px, 50.f*py)
    new PointF(60.f*px, 34.f*py)
    new PointF(50.f*px, 34.f*py)
    new PointF(50.f*px, 8.f*py)
    new PointF(21.f*px, -7.f*py)
    new PointF(1.f*px,  6.f*py)
    new PointF(7.f*px,  14.f*py)
    new PointF(27.f*px, 5.f*py)
    new PointF(40.f*px, 23.f*py)
  |]
  let arrow_anticlockwise = mirroring(arrow_clockwise, 60.f*px)
  let arrow =
    match dir with
      | Clockwise ->      arrow_clockwise
      | Anticlockwise ->  arrow_anticlockwise

  // Vars
  let arrowpath        = new GraphicsPath()
  let mutable realpath = new GraphicsPath()

  // Constructor
  do
    arrowpath.AddLine(arrow.[0], arrow.[1])
    arrowpath.AddLine(arrow.[1], arrow.[2])
    arrowpath.AddLine(arrow.[2], arrow.[3])
    arrowpath.AddLine(arrow.[3], arrow.[4])
    arrowpath.AddBezier(arrow.[4], arrow.[5], arrow.[6], arrow.[7])
    arrowpath.AddLine(arrow.[7], arrow.[8])
    arrowpath.AddBezier(arrow.[8], arrow.[9], arrow.[10], arrow.[0])
    // Calculate the path with the real coordinates
    realpath <- arrowpath.Clone() :?> GraphicsPath
    let m = new Matrix()
    m.Translate(x, y)
    realpath.Transform(m)

  override this.OnPaint g =
    // Graphical elements

    g.FillPath(Brushes.Black, arrowpath)

  member this.Dir = dir

  static member TakeDir(dir) :
    float32 =
      let rotate_factor = 1.f
      match dir with
        | Clockwise ->      rotate_factor
        | Anticlockwise ->  -rotate_factor

  override this.Contains p =
    realpath.IsVisible(p)

///////////////////////////////////
//////////  PLAY BUTTON  //////////
///////////////////////////////////
type PlayButton(x, y, w, h) =
  inherit Button(x, y, w, h)

  // Vars
  let mutable string = "PLAY"
  let mutable play = false

  member this.Play
    with get() = play
    and set(v) =
      play <- v
      string <- if play then "STOP" else "PLAY"
  
  override this.OnPaint g =
    // Graphical elements
    use font = new Font(FontFamily.GenericSansSerif, 10.0f, FontStyle.Bold)

    // Draw box
    g.FillRectangle(Brushes.Black, 0.f, 0.f, w, h)

    // Calculate center position for the text, and draw the string
    let sz = g.MeasureString(string, font)
    let pos  = PointF((w - sz.Width) / 2.f, (h - sz.Height) / 2.f)
    g.DrawString(string, font, Brushes.White, pos)

  override this.Contains p =
    this.Rectangle.Contains(p)
