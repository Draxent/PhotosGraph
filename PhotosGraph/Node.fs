namespace IUM.MidTerm

open System.Drawing
open System.Drawing.Drawing2D
open IUM.MidTerm

type Node() =
  //////////////////////////////
  //////////  DEFINE  //////////
  //////////////////////////////
  let density           = 0.657f // Red Oak density 0.657 g/cm³
  let pi                = float32 System.Math.PI
  let height_circle     = 0.1f
  let sel_border        = 7.f

  ////////////////////////////
  //////////  VARS  //////////
  ////////////////////////////
  // Property
  let mutable center      = new PointF()
  let mutable radius      = 0.f
  let mutable mass        = 0.f
  let mutable path_image  = ""
  let mutable selected    = false
  
  // Current velocity, force, acceleration 
  let mutable v = new Vector()
  let mutable f = new Vector()
  let mutable a = new Vector()

  /////////////////////////////////
  //////////  FUNCTIONS  //////////
  /////////////////////////////////
  let RotateAtTransform(g:Graphics, angle:float32, p:PointF) =
    g.TranslateTransform(p.X, p.Y)
    g.RotateTransform(angle)
    g.TranslateTransform(-p.X, -p.Y)

  // Draw the string with the same angle of the circle radius
  let DrawAngularString(g:Graphics, s:string, f:Font, brush:Brush, angle:float32, c:PointF, p:PointF, value:float32, up:bool) =
    // Calculate the pos of the string
    let sz = g.MeasureString(s, f)
    let shift = Vector.Direction(c, p, value, -value)
    let h = if up then - sz.Height else 0.f
    // Save and transform the Graphic space
    let gs = g.Save()
    RotateAtTransform(g, Vector.RadiansToDegrees(angle), c)
    // Draw the string and restore the Graphic state
    g.DrawString(s, f, brush, new PointF(c.X + shift - sz.Width/2.f, c.Y + h))
    g.Restore(gs)

  //////////////////////////////
  //////////  MEMBER  //////////
  //////////////////////////////
  // Trasform Rectangle
  static member TransformRectangle(w2v:Matrix, r:RectangleF) =
    let bbox = [| new PointF(r.Left, r.Top); new PointF(r.Right, r.Top); new PointF(r.Right, r.Bottom); new PointF(r.Left, r.Bottom) |]
    w2v.TransformPoints(bbox)
    use path = new GraphicsPath()
    path.AddPolygon(bbox)
    new Region(path)

  // Center
  member this.Center
    with get() = center
    and set(c) = center <- c
  member this.X
    with get() = center.X
    and set(x) = center.X <- x
  member this.Y
    with get() = center.Y
    and set(y) = center.Y <- y

  // Radius
  member this.Radius
    with get() = radius
    and set(r) =
      radius <- r
      let surface_area = pi * r * r
      let volume = surface_area*height_circle
      mass <- density * volume

  // Mass: red oak cylinder
  member this.Mass
    with get() = mass

  // Speed
  member this.V
    with get()  = v
    and set(vel) = v <- vel

  // Force
  member this.F
    with get()  = f
    and set(v) = f <- v

  // Acceleration
  member this.A
    with get()  = f / mass

  // Selected
  member this.Selected
    with get() = selected
    and set(v) = selected <- v

  // Rectangle containing the node
  member this.Rectangle =
    new RectangleF(center.X - radius, center.Y - radius, 2.f * radius, 2.f * radius)
  
  // Clone the node
  member this.Clone() =
    new Node(Center = this.Center, Radius = this.Radius)

  // Obtain the distance between the center of the node and the point $p
  member this.Distance(p:PointF) =
    sqrt((center.X - p.X) ** 2.f + (center.Y - p.Y) ** 2.f)

  // If point $p is contained in the node
  member this.Contains(p:PointF) =
    (this.Distance(p) <= radius)

  // Set the image thanks to the path
  member this.PathImage
    with get() = path_image
    and set(v) = path_image <- v

  // Invalidate the node
  member this.Invalidate(w2v:Matrix, rad:float32) =
    Node.TransformRectangle(w2v, new RectangleF(this.X - rad - 1.f, this.Y - rad - 1.f, 2.f*rad + 2.f, 2.f*rad + 2.f))

  // Invalidate the moviment of the node
  member this.MovimentInvalidate(w2v:Matrix, scale:float32, c1:PointF, c2:PointF) =
    let border_error = sel_border / scale + 1.f
    let (left, right) = ((min c1.X c2.X) - radius - border_error, (min c1.Y c2.Y) - radius - border_error)
    let (width, height) = (2.f*radius + abs(c1.X - c2.X) + 2.f*border_error, 2.f*radius + abs(c1.Y - c2.Y) + 2.f*border_error)
    Node.TransformRectangle(w2v, new RectangleF(left, right, width, height))

  // Draw the node
  member this.Paint(g:Graphics, scale:float32, bg:Image, ?p:PointF) =
    // Graphical elements
    use border_pen = new Pen(Color.Black, 0.f)
    use selborder_pen = new Pen(Color.DarkOrange, sel_border/scale)
    use radius_pen = new Pen(Color.Red, 1.f/scale)
    radius_pen.DashStyle <- DashStyle.Dash
    use radius_font = new Font("Times new Roman", 12.f/scale)

    let rad = radius*scale

    // Draw circle
    let circle_brush = if p.IsSome then Brushes.Orange else Brushes.White
    let circle_pen = if selected then selborder_pen else border_pen
    g.DrawEllipse(circle_pen, this.Rectangle)
    if bg = null then
      g.FillEllipse(circle_brush, this.Rectangle)
    else
      g.DrawImage(bg, this.Rectangle.X, this.Rectangle.Y)

    // Creation phase -> Draw radius
    if (p.IsSome && (rad >= 1.f)) then
      let string_radius = sprintf "%.1f cm" radius
      let string_mass = sprintf "%.1f kg" (mass / 1000.f)
      let angle = Vector.Angular_Coefficient(center, p.Value)
      // Draw line
      let gs = g.Save()
      RotateAtTransform(g, Vector.RadiansToDegrees(angle), center)
      let shift = Vector.Direction(center, p.Value, radius, -radius)
      g.DrawLine(radius_pen, center, new PointF(center.X + shift, center.Y))
      g.Restore(gs)
      // Draw strings
      DrawAngularString(g, string_radius, radius_font, Brushes.Red, angle, center, p.Value, radius/2.f, true)
      DrawAngularString(g, string_mass, radius_font, Brushes.Red, angle, center, p.Value, radius/2.f, false)

