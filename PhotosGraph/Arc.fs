namespace IUM.MidTerm

open System.Drawing
open System.Drawing.Drawing2D
open IUM.MidTerm

type Arc() =
  //////////////////////////////
  //////////  DEFINE  //////////
  //////////////////////////////
  let pi                    = float32 System.Math.PI
  let clickable_area_angle  = Vector.DegreesToRadians(3.f)
  let elastic_constant      = 400.0f // K = 400 Nm
  let damped_factor          = 30.f

  ////////////////////////////
  //////////  VARS  //////////
  ////////////////////////////
  // Property
  let mutable (start_node:Node option) = None
  let mutable (end_node:Node option)   = None
  let mutable text                     = "write here..."
  let mutable visible_text             = false
  let mutable clickable_area           = new GraphicsPath()
  let mutable text_area                = new GraphicsPath()
  let mutable selected                 = false
  let mutable resting_lenght           = 0.f // resting length of the spring in m

  /////////////////////////////////
  //////////  FUNCTIONS  //////////
  /////////////////////////////////
  let RotateAtTransform(g:Graphics, angle:float32, p:PointF) =
    g.TranslateTransform(p.X, p.Y)
    g.RotateTransform(angle)
    g.TranslateTransform(-p.X, -p.Y)

  // Calculate the shifting to calculate the points on the $start_node and $end_node circumferences and draw the link line
  let AngularShift(angle:float32, varangle:float32) =
    let mutable shift = [| 0.f; 0.f; 0.f; 0.f |]
    if start_node.IsSome && end_node.IsSome then
      let (snode, enode) = (start_node.Value, end_node.Value)
      let (angle1, angle2) = (angle + varangle, angle - varangle)
      // Shift (x, y) respectively for $snode and $enode
      shift <- [| snode.Radius*cos(angle1); snode.Radius*sin(angle1); enode.Radius*cos(angle2); enode.Radius*sin(angle2) |]
      if (enode.X - snode.X >= 0.f) then shift.[2] <- (-shift.[2]); shift.[3] <- (-shift.[3])
      else shift.[0] <- (-shift.[0]); shift.[1] <- (-shift.[1])
    shift

  // Draw the arrow with the same angle of the link between nodes
  let DrawArrow(g:Graphics, startarrow:bool, dirarrow:PointF[][], brush:Brush, angle:float32, c:PointF, p:PointF) =
    // Save and transform the Graphic space
    let gs = g.Save()
    g.TranslateTransform(c.X, c.Y)
    g.RotateTransform(Vector.RadiansToDegrees(angle))
    // Mirroring the arrow depending of the mouse_point
    let (index, neq) = if startarrow then (0, false) else (2, true)
    let arrow = Vector.Direction(c, p, dirarrow.[index], dirarrow.[index + 1], neq)
    // Draw the arrow and restore the Graphic state
    g.FillPolygon(brush, arrow)
    g.Restore(gs)

  // Draw the string with the same angle of the link between nodes
  let DrawAngularString(g:Graphics, s:string, f:Font, bgbursh:Brush, brush:Brush, borderpen:Pen, angle:float32, c:PointF, p:PointF, lenght:float32) =
    text_area.Reset()
    // Calculate the pos of the string
    let sz = g.MeasureString(s, f)
    let shift = Vector.Direction(c, p, lenght/2.f, -lenght/2.f)
    let (posx, posy) = (c.X + shift - sz.Width/2.f, c.Y - sz.Height)
    if (sz.Width < lenght) then
      // Calculate the clickable area
      let m = new Matrix()
      let rect = new RectangleF(posx, posy, sz.Width, 9.f/10.f*sz.Height)
      m.RotateAt(Vector.RadiansToDegrees(angle), c)
      text_area.AddRectangle(rect)
      text_area.Transform(m)
      g.FillPath(bgbursh, text_area)
      g.DrawPath(borderpen, text_area)
      // Save and transform the Graphic space
      let gs = g.Save()
      RotateAtTransform(g, Vector.RadiansToDegrees(angle), c)
      // Draw the string and restore the Graphic state
      g.DrawString(s, f, brush, new PointF(posx, posy)) 
      g.Restore(gs)


  //////////////////////////////
  //////////  MEMBER  //////////
  //////////////////////////////
  // StartNode
  member this.StartNode
    with get() = start_node
    and set(v) = start_node <- v

  // EndNode
  member this.EndNode
    with get() = end_node
    and set(v) = end_node <- v

  // Selected
  member this.Selected
    with get() = selected
    and set(v) = selected <- v

  // Selected
  member this.VisibleText
    with get() = visible_text
    and set(v) = visible_text <- v

  // Text
  member this.Text
    with get() = text
    and set(v) = text <- v

  //Ends of the link line between nodes
  member this.EndsOfLine
    with get() =
      if start_node.IsSome && end_node.IsSome then
        let (snode, enode) = start_node.Value, end_node.Value
        let angle = Vector.Angular_Coefficient(snode.Center, enode.Center)
        let shift = AngularShift(angle, 0.f)
        (new PointF(snode.X + shift.[0], snode.Y + shift.[1]), new PointF(enode.X + shift.[2], enode.Y + shift.[3]))
      else
        (new PointF(), new PointF())

  // Lenght
  member this.Lenght
    with get() =
      if start_node.IsSome && end_node.IsSome then
        let (pstart, pend) = this.EndsOfLine
        Vector.Distance(pstart, pend)
      else
        0.f

  member this.Center
    with get() =
      let (pstart, pend) = this.EndsOfLine
      new PointF((pstart.X + pend.X)/2.f, (pstart.Y + pend.Y)/2.f)

  // Resting Lenght
  member this.RestingLenght
    with get() = resting_lenght
    and set(v) = resting_lenght <- v

  // Angle link
  member this.AngleLink
    with get() =
      if start_node.IsSome && end_node.IsSome then
        Vector.Angular_Coefficient(start_node.Value.Center, end_node.Value.Center)
      else
        0.f

  // Hooke Force
  member this.HookeForce(n:Node) =
    let mod_deltax = (this.Lenght - resting_lenght)/2.f
    let ang_deltax = if mod_deltax >= 0.f then this.AngleLink else this.AngleLink + pi
    let deltax = new Vector(Module = abs(mod_deltax), Angle = ang_deltax)
    let force = deltax * elastic_constant
    let damping = n.V * damped_factor
    Vector.Direction(this.Center, n.Center, -force-damping, force-damping)
      
  // Check if the arc is already inside the $set and if the arc is correct
  member this.Exist(set:ResizeArray<Arc>) =
    let exist = ref(false)
    // Check if the arc points to the same node
    if this.StartNode = this.EndNode then
      exist := true
    else
      set |> Seq.iter(fun elem ->
        if ((elem.StartNode = start_node) && (elem.EndNode = end_node)) || ((elem.StartNode = end_node) && (elem.EndNode = start_node)) then
          exist := true
      )
    !exist

  // If point $p is contained in the arc
  member this.Contains(p:PointF) =
    clickable_area.IsVisible(p)

  // If point $p is contained in the text area
  member this.TextContains(p:PointF) =
    text_area.IsVisible(p)

  // Draw the arc
  member this.Paint(g:Graphics, scale:float32, update_restinglenght:bool, ?p:PointF) =
    // Graphical elements
    use text_font = new Font("Times new Roman", 11.f/scale, FontStyle.Italic)
    use arrow_dashpen = new Pen(Brushes.Black, 2.f/scale, DashStyle = DashStyle.Dash)
    use textarea_pen = new Pen(Brushes.Gray, 0.5f/scale, DashStyle = DashStyle.Dash)
    use arrow_pen = new Pen(Brushes.Black, 2.f/scale)
    use selarrow_pen = new Pen(Brushes.Green, 4.f/scale)
    let pen = if selected then selarrow_pen else arrow_pen

    // Arrows
    let start_left_arrow  = [| new PointF(0.f, -10.f/scale);          new PointF(0.f, 10.f/scale);          new PointF(-10.f/scale, 0.f) |]
    let start_right_arrow = [| new PointF(0.f, -10.f/scale);          new PointF(0.f, 10.f/scale);          new PointF(10.f/scale, 0.f)  |]
    let end_left_arrow    = [| new PointF(10.f/scale, -10.f/scale);   new PointF(10.f/scale, 10.f/scale);   new PointF(0.f, 0.f)   |]
    let end_right_arrow   = [| new PointF(-10.f/scale, -10.f/scale);  new PointF(-10.f/scale, 10.f/scale);  new PointF(0.f, 0.f)   |]
    let dirarrow =  [| start_left_arrow; start_right_arrow; end_left_arrow; end_right_arrow |]

    // Creation phase
    if p.IsSome then
      let mouse_point = p.Value
      if start_node.IsSome then
        let snode = start_node.Value
        let angle = Vector.Angular_Coefficient(snode.Center, mouse_point)
        let startpoint = new PointF(snode.X, snode.Y)
        DrawArrow(g, true, dirarrow, Brushes.Black, angle, startpoint, mouse_point)
        DrawArrow(g, false, dirarrow, Brushes.Black, angle, mouse_point, startpoint)
        g.DrawLine(arrow_dashpen, snode.Center, mouse_point)
    elif (start_node.IsSome) && (end_node.IsSome) then
      // Change the resting lenght only in creation mode
      if update_restinglenght then
        resting_lenght <- this.Lenght

      // Draw the link line
      let (snode, enode) = start_node.Value, end_node.Value
      let angle = Vector.Angular_Coefficient(snode.Center, enode.Center)
      let (pstart, pend) = this.EndsOfLine
      g.DrawLine(pen, pstart, pend)

      if visible_text then
        DrawAngularString(g, text, text_font, Brushes.White, Brushes.Black, textarea_pen, angle, pstart, pend, this.Lenght)

      // Calculate the clickable area
      let shift = AngularShift(angle, clickable_area_angle)
      let (p1, p2) = (new PointF(snode.X + shift.[0], snode.Y + shift.[1]), new PointF(enode.X + shift.[2], enode.Y + shift.[3]))
      let shift = AngularShift(angle, -clickable_area_angle)
      let (p4, p3) = (new PointF(snode.X + shift.[0], snode.Y + shift.[1]), new PointF(enode.X + shift.[2], enode.Y + shift.[3]))
      clickable_area.Reset()
      clickable_area.AddLine(p1, p2)
      clickable_area.AddLine(p2, p3)
      clickable_area.AddLine(p3, p4)
      clickable_area.AddLine(p4, p1)