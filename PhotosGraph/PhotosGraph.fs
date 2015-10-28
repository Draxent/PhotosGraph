namespace IUM.MidTerm

open System
open System.Windows.Forms
open System.Drawing
open System.Drawing.Drawing2D
open System.IO
open IUM.MidTerm
open IUM.MidTerm.UsefulFunctions

type PhotosGraph() as this =
  inherit UserControl()

  //////////////////////////////
  //////////  DEFINE  //////////
  //////////////////////////////
  let pannel_size   = new Size(120, 120)
  let speed_factor  = 5.0f
  let euler_steps   = 1 // Number of steps for the Improved Euler's Method

  ////////////////////////////
  //////////  VARS  //////////
  ////////////////////////////
  let mutable mouse_point     = new PointF() // Mouse coordinates
  let mutable mouse_pressed   = false
  // Graph vars
  let nodes = new ResizeArray<Node>() 
  let arcs  = new ResizeArray<Arc>()
  // Graph control
  let imagecache                = new ImageCache(nodes)
  let mutable newnode           = Node()
  let mutable newarc            = Arc()
  let mutable creation_node     = false
  let mutable creation_arc      = false
  let mutable sel_node          = None // Node selected
  let mutable sel_arc           = None // Arc selected
  let mutable mouse_offset      = new SizeF() // Offset between the Clicked Node center and the mouse
  let mutable (textbox:TextBox) = null
  let mutable indexext          = 1
  // Interface Buttons
  let scroll_buttons = [|
    new ScrollButton(Left,  5.f,   40.f,   20.f,   40.f);
    new ScrollButton(Up,    40.f,   5.f,   40.f,   20.f);
    new ScrollButton(Right, 95.f,  40.f,   20.f,   40.f);
    new ScrollButton(Down,  40.f,   95.f,  40.f,   20.f)
  |]
  let zoom_buttons = [|
    new ZoomButton(ZoomIn,  8.f,   87.f,   25.f,   25.f);
    new ZoomButton(ZoomOut, 87.f,   87.f,   25.f,   25.f)
  |]
  let rotate_buttons = [|
    new RotateButton(Anticlockwise,   5.f,   5.f,   35.f,   35.f);
    new RotateButton(Clockwise,       80.f,   5.f,   35.f,   35.f)
  |]
  let play_button = new PlayButton(35.f, 40.f, 50.f, 40.f)
  let interfaceTimer          = new Timer(Interval = 50)
  let mutable scroll_action   = (false, new PointF())
  let mutable zoom_action     = (false, 0.f)
  let mutable rotation_action = (false, 0.f)
  let pannel_area             = new GraphicsPath()
  // Matrix
  let mutable scale = 1.f // Current scaling of the view
  let w2v           = new Matrix() // world to view
  let v2w           = new Matrix() // view to worl
  // Animation
  let mutable moving_node   = false
  let mutable animation     = false
  let timer                 = new System.Diagnostics.Stopwatch()
  let mutable refreshTime   = 0.f // time used from the last OnPaint
  let mutable total_speed   = (0.f, 0)

  /////////////////////////////////
  //////////  FUNCTIONS  //////////
  /////////////////////////////////
  let PlayPressed() =
    if animation && not(moving_node) then
      StopSystem(nodes, arcs)
    animation <- not(animation)
    play_button.Play <- not(play_button.Play)
    this.Invalidate() 

  // Check if some interface button is been clicked and execute the corrisponding action
  let InterfaceButton_Contain(p:PointF) =
    let mutable res = false
    for b in scroll_buttons do
      if b.Contains(p) then
        res <- true
        scroll_action <- (true, ScrollButton.TakeDir(b.Dir))
        interfaceTimer.Start()
    for b in zoom_buttons do
      if b.Contains(p) then
        res <- true
        zoom_action <- (true, ZoomButton.TakeDir(b.Dir))
        interfaceTimer.Start()
    for b in rotate_buttons do
      if b.Contains(p) then
        res <- true
        rotation_action <- (true, RotateButton.TakeDir(b.Dir))
        interfaceTimer.Start()
    if play_button.Contains(p) then
      res <- true
      PlayPressed()
    res

  let AddNewNode() =
    // Open the dialog window to select the image for the node
    let dialog = new OpenFileDialog()
    dialog.InitialDirectory <- Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName, "Images")
    dialog.Filter <- "JPG Files (*.jpg)|*.jpg|PNG Files (*.png)|*.png|GIF Files (*.gif)|*.gif"
    dialog.FilterIndex <- indexext;
    if dialog.ShowDialog() = DialogResult.OK then
      // Load the image
      newnode.PathImage <- dialog.FileName
      nodes.Add(newnode)
      newnode <- new IUM.MidTerm.Node()
      match Path.GetExtension(dialog.FileName) with
      | ".jpg" -> indexext <- 1
      | ".png" -> indexext <- 2
      | ".gif" -> indexext <- 3
      | _ -> indexext <- 1
      this.Invalidate()
    else
      // Delete new node
      newnode.Radius <- 0.f
      this.Invalidate()

  let AddNewArc() =
    DeselectAllNodes(nodes, newarc, false)
    // Check if the mouse is been released over a node
    let released_node = NodesContains(nodes, mouse_point)
    if released_node.IsSome then
      newarc.EndNode <- Some(nodes.[released_node.Value])
    if (released_node.IsNone) || (newarc.Exist(arcs)) then
      // Delete the new arc if it isn't complete, points to the same node or exists already
      newarc.StartNode <- None
      newarc.EndNode <- None
      this.Invalidate()
    else
      // Add the new arc
      arcs.Add(newarc)
      newarc <- new IUM.MidTerm.Arc()
      this.Invalidate()

  ///////////////////////////////////
  //////////  CONSTRUCTOR  //////////
  ///////////////////////////////////
  do
    // Activate Double Buffering
    this.SetStyle(ControlStyles.AllPaintingInWmPaint, true)
    this.SetStyle(ControlStyles.DoubleBuffer, true)
    // Interface Buttons effects
    interfaceTimer.Tick.Add(fun _ ->
      let (activated_scroll, scrollDir) = scroll_action
      let (activated_zoom, zoomDir) = zoom_action
      let (activated_rotation, rotationDir) = rotation_action
      if activated_scroll then
        let scroll = TransformVector(v2w, scrollDir)
        w2v.Translate(scroll.X, scroll.Y)
        v2w.Translate(-scroll.X, -scroll.Y, Drawing2D.MatrixOrder.Append)
        pannel_area.Transform(v2w)
        this.Invalidate()
      elif (activated_zoom && (zoomDir <> 0.f)) then
        let center = TransformPoint(v2w, new PointF(float32 this.ClientSize.Width / 2.f, float32 this.ClientSize.Height / 2.f))
        // ScaleAt($s, $c) = Translate($c); Scale($s); Translate(-$c)
        w2v.Translate(center.X, center.Y)
        w2v.Scale(zoomDir, zoomDir)
        w2v.Translate(-center.X, -center.Y)
        v2w.Translate(-center.X, -center.Y, Drawing2D.MatrixOrder.Append)
        v2w.Scale(1.f/zoomDir, 1.f/zoomDir, Drawing2D.MatrixOrder.Append)
        v2w.Translate(center.X, center.Y, Drawing2D.MatrixOrder.Append)
        scale <- scale*zoomDir
        this.Invalidate()
      elif activated_rotation then
        let center = TransformPoint(v2w, new PointF(float32 this.ClientSize.Width / 2.f, float32 this.ClientSize.Height / 2.f))
        w2v.RotateAt(rotationDir, center)
        v2w.RotateAt(-rotationDir, center, Drawing2D.MatrixOrder.Append)
        this.Invalidate()
    )
    // Pannel Area
    pannel_area.AddRectangle(new Rectangle(0, 0, 120, 120))

  ////////////////////////////////////
  //////////   ONMOUSEDOWN  //////////
  ////////////////////////////////////
  override this.OnMouseDown e =
    mouse_point <- TransformPoint(v2w, new PointF(float32 e.X, float32 e.Y))

    // Reset selected stuff
    sel_node <- None
    DeselectAllNodes(nodes, newarc, false)
    sel_arc <- None
    DeselectAllArcs(arcs)
    // Delete textbox
    if textbox <> null then
      this.Focus() |> ignore
      this.Controls.Remove(textbox)

    // Check if some interface button is been clicked and execute the corrisponding action
    if not(InterfaceButton_Contain(new PointF(float32 e.X, float32 e.Y))) && not((e.X < pannel_size.Width) && (e.Y < pannel_size.Height)) then
      // Check if a node is pressed
      let pressed_node = NodesContains(nodes, mouse_point)
      let pressed_arc = ArcsContains(arcs, mouse_point)
      let pressed_text = ArcsTextContains(arcs, mouse_point)
      match (pressed_node, pressed_arc, pressed_text) with
        // A node is been pressed
        | (Some(i), _, _) ->
          if e.Button = MouseButtons.Left then
            // Selected the Node
            mouse_pressed <- true
            sel_node <- pressed_node
            nodes.[i].Selected <- true
            mouse_offset <- new SizeF(nodes.[i].X - mouse_point.X, nodes.[i].Y - mouse_point.Y)
            //Stop animation while the node is moving around
            if animation then
              moving_node <- true
              PlayPressed()
            this.Invalidate()
          elif (e.Button = MouseButtons.Right && not(animation)) then
            // Create a new Arc
            creation_arc <- true
            nodes.[i].Selected <- true
            newarc.StartNode <- Some(nodes.[i])
            this.Invalidate()
        // An arc is been pressed
        | (_, Some(i), _) ->
          if (e.Button = MouseButtons.Left && not(animation)) then
            // Selected the Arc
            sel_arc <- pressed_arc
            arcs.[i].Selected <- true
            this.Invalidate()
        // The text area of an arc is been pressed
        | (_, _, Some(i)) ->
          if (e.Button = MouseButtons.Left && not(animation)) then
            // Change the text
            textbox <- new TextBox(Text = arcs.[i].Text)
            textbox.TextChanged.Add(fun _ -> arcs.[i].Text <- textbox.Text)
            textbox.Dock <- DockStyle.Right
            this.Controls.Add(textbox)
            textbox.Focus() |> ignore
        // Nothing is been pressed
        | (None, None, None) ->
          if (e.Button = MouseButtons.Left && not(animation)) then
            // Create a new Node
            creation_node <- true
            newnode.Center <- mouse_point
            this.Invalidate()
          elif (e.Button = MouseButtons.Right && not(animation)) then
            sel_node <- None
            sel_arc <- None
            this.Invalidate()

  ////////////////////////////////////
  //////////   ONMOUSEUP  //////////
  ////////////////////////////////////
  override this.OnMouseUp _ =
    // Stop the interfaceButtons effect
    scroll_action <- (false, new PointF())
    zoom_action <- (false, 0.f)
    rotation_action <- (false, 0.f)
    interfaceTimer.Stop()

    // Restart animation
    if moving_node then
      moving_node <- false
      PlayPressed()

    // Compleate the creation actions
    mouse_pressed <- false
    creation_node <- false
    creation_arc <- false
    if newnode.Radius > 0.f then
      AddNewNode() // Add the new node
    elif newarc.StartNode.IsSome then
      AddNewArc() // Add the new arc

  ////////////////////////////////////
  //////////   ONMOUSEMOVE  //////////
  ////////////////////////////////////
  override this.OnMouseMove e =
    mouse_point <- TransformPoint(v2w, new PointF(float32 e.X, float32 e.Y))

    if creation_node then
      // Calculate the maximum space for the newnode's radius
      let maxrad = MaximumSpace(nodes, newnode)
      let oldrad = newnode.Radius
      // Change the radius of the new node
      newnode.Radius <- min (newnode.Distance(mouse_point)) maxrad
      //this.Invalidate(newnode.Invalidate(w2v, max newnode.Radius oldrad))
      this.Invalidate()
    elif creation_arc then
      // If mouse is over a node, selected that node
      let over_node = NodesContains(nodes, mouse_point)
      if over_node.IsSome then nodes.[over_node.Value].Selected <- true
      else DeselectAllNodes(nodes, newarc, true)
      this.Invalidate()
    elif mouse_pressed && sel_node.IsSome then
      let snode = nodes.[sel_node.Value]
      let oldcenter = snode.Center
      let newcenter = new PointF(mouse_point.X + mouse_offset.Width, mouse_point.Y + mouse_offset.Height)
      // Change the center of the selected node
      snode.Center <- newcenter
      //this.Invalidate(snode.MovimentInvalidate(w2v, scale, oldcenter, newcenter))
      this.Invalidate()

  //////////////////////////////////
  //////////   ONKEYDOWN  //////////
  //////////////////////////////////
  override this.OnKeyDown e =
    match e.KeyCode with
      | Keys.A | Keys.W | Keys.D | Keys.S ->
        match sel_node with
          // Move the node
          | Some(i) ->
            let dir =
              match e.KeyCode with
                | Keys.A -> Right | Keys.W -> Down | Keys.D -> Left | Keys.S | _ -> Up
            let scroll = TransformVector(v2w, ScrollButton.TakeDir(dir))
            nodes.[i].Center <- new PointF(nodes.[i].X + scroll.X, nodes.[i].Y + scroll.Y)
            this.Invalidate()
          // Transform the view
          | None ->
            let dir =
              match e.KeyCode with
                | Keys.A -> Left | Keys.W -> Up | Keys.D -> Right | Keys.S | _ -> Down
            scroll_action <- (true, ScrollButton.TakeDir(dir))
            interfaceTimer.Start()
      | Keys.Z | Keys.X ->
        let dir =
          match e.KeyCode with
            | Keys.Z -> ZoomIn | Keys.X | _ -> ZoomOut
        zoom_action <- (true, ZoomButton.TakeDir(dir))
        interfaceTimer.Start()
      | Keys.Q | Keys.E ->
        let dir =
          match e.KeyCode with
            | Keys.Q -> Anticlockwise | Keys.E | _ -> Clockwise
        rotation_action <- (true, RotateButton.TakeDir(dir))
        interfaceTimer.Start()
      | Keys.Enter -> PlayPressed()
      | Keys.T ->
        match sel_arc with
          | Some(i) ->
            arcs.[i].VisibleText <- not(arcs.[i].VisibleText)
            this.Invalidate()
          | None -> ()
      | Keys.Delete ->
        match sel_node with
          | Some(i) ->
            if not(animation) then
              RemoveNode(nodes, arcs, i)
              sel_node <- None
              this.Invalidate()
          | None -> ()
        match sel_arc with
          | Some(i) ->
            arcs.RemoveAt(i)
            sel_arc <- None
            this.Invalidate()
          | None -> ()
      | _ -> ()

  ////////////////////////////////
  //////////   ONKEYUP  //////////
  ////////////////////////////////
  override this.OnKeyUp e =
    // Stop the interfaceButtons effect
    scroll_action <- (false, new PointF())
    zoom_action <- (false, 0.f)
    rotation_action <- (false, 0.f)
    interfaceTimer.Stop()

  ////////////////////////////////
  //////////   ONPAINT  //////////
  ////////////////////////////////
  override this.OnPaint e =
    let g = e.Graphics
    g.SmoothingMode <- SmoothingMode.AntiAlias

    timer.Restart() // Start timer

    // Graphical elements
    use bg_interfacebuttons = new SolidBrush(Color.FromArgb(220, Color.LightGray))

    g.Transform <- w2v

    let screen_region = ScreenRegion(this.ClientRectangle, v2w)

    // Draw arcs
    arcs |> Seq.iter(fun a -> a.Paint(g, scale, not(moving_node) && not(animation)))

    // Draw nodes
    if newnode.Radius > 0.f then
      newnode.Paint(g, scale, null, mouse_point)
    nodes |> Seq.iter(fun n ->
      if screen_region.IsVisible(imagecache.GetImageBounds(n)) then
        imagecache.SetVisible(n) true
        n.Paint(g, scale, CircleImage(imagecache.GetImage(n), int n.Radius))
      else
        imagecache.SetVisible(n) false
    )

    // Draw new arc
    if newarc.StartNode.IsSome then
      newarc.Paint(g, scale, not(moving_node) && not(animation), mouse_point)

    g.ResetTransform()

    // Draw Interface buttons
    g.FillRectangle(bg_interfacebuttons, 0, 0, pannel_size.Width, pannel_size.Height)
    g.DrawRectangle(Pens.Black, 0, 0, pannel_size.Width, pannel_size.Height)
    for b in scroll_buttons do b.Paint(g)
    for b in zoom_buttons do b.Paint(g)
    for b in rotate_buttons do b.Paint(g)
    play_button.Paint(g)

    timer.Stop() // Stop timer
    refreshTime <- float32(timer.ElapsedMilliseconds) / 1000.0f

  ////////////////////////////////
  //////////   ANIMATE  //////////
  ////////////////////////////////
  member this.Animate() =
    if animation then
      // Improved Euler's Method: increased the number of steps
      let delta = (1.0f / float32(euler_steps)) * speed_factor
      // Accumulate all the acceleration to avoid the Euler's Method if it is equal to zero
      let mutable total_force = 0.f
      // Calculate the acceleration for each node
      for node in nodes do
        node.F <- HookeForce(arcs, node)
        total_force <- total_force + node.F.Module

      if total_force > 0.f then
        for i = 1 to euler_steps do
          // Calculate the speed change and the position change in this step
          for node in nodes do
            node.V <- node.V + node.A * delta * refreshTime
            node.X <- node.X + node.V.X * delta * refreshTime
            node.Y <- node.Y + node.V.Y * delta * refreshTime
              
        // Calculate the average speed of the nodes
        let mutable average_speed = 0.f
        for node in nodes do
          average_speed <- average_speed + node.V.Module
        average_speed <- average_speed / float32 nodes.Count
        
        // If the totalspeed of 100 animations is too little, stop the system
        let (t, i) = total_speed
        total_speed <- (t + average_speed, i+1)
        if ((i = 100) && (t + average_speed < 100.f)) then PlayPressed()
        elif i >= 100 then total_speed <- (0.f, 0)

        // Update the screen
        this.Invalidate()