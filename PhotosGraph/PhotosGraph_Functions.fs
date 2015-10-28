namespace IUM.MidTerm

open System.Drawing
open System.Drawing.Drawing2D
open IUM.MidTerm

module UsefulFunctions =
  // Trasform a single point $p using the matrix $m
  let TransformPoint(m:Matrix, p:PointF) =
    let pl = [| p |]
    m.TransformPoints(pl)
    pl.[0]
      
  // Trasform vectorially a single point $p using the matrix $m
  let TransformVector(m:Matrix, v:PointF) =
    let vl = [| v |]
    m.TransformVectors(vl)
    vl.[0]

  // Check if p is contained in a node
  let NodesContains(nodes:ResizeArray<Node>, p:PointF) =
    let res = ref(None)
    nodes |> Seq.iteri(fun i n ->
      if n.Contains(p) then
        res := Some i
    )
    !res

  // Check if p is contained in an arc
  let ArcsContains(arcs:ResizeArray<Arc>, p:PointF) =
    let res = ref(None)
    arcs |> Seq.iteri(fun i a ->
      if a.Contains(p) then
        res := Some i
    )
    !res

  // Check if p is contained in the textarea of an arc
  let ArcsTextContains(arcs:ResizeArray<Arc>, p:PointF) =
    let res = ref(None)
    arcs |> Seq.iteri(fun i a ->
      if a.TextContains(p) then
        res := Some i
    )
    !res
    
  // Deselect All Nodes, except newarc.StartNode if the flag is true
  let DeselectAllNodes(nodes:ResizeArray<Node>, newarc:Arc, except:bool) =
    nodes |> Seq.iter(fun n ->
      if not(except && newarc.StartNode.IsSome && newarc.StartNode.Value = n) then n.Selected <- false
    )

  // Deselect All Arcs
  let DeselectAllArcs(arcs:ResizeArray<Arc>) =
    arcs |> Seq.iter(fun a -> a.Selected <- false)

  // Remove the node in pos $index
  let RemoveNode(nodes:ResizeArray<Node>, arcs:ResizeArray<Arc>, index:int) =
    let deletearcs = new ResizeArray<IUM.MidTerm.Arc>()
    arcs |> Seq.iter(fun a ->
      if (a.StartNode = Some(nodes.[index])) || (a.EndNode = Some(nodes.[index])) then
        deletearcs.Add(a)
    )
    deletearcs |> Seq.iter(fun a -> arcs.Remove(a) |> ignore)
    nodes.RemoveAt(index)

  // Calcolate the region of the screen
  let ScreenRegion(rect:Rectangle, v2w:Matrix) =
    let bbox = [| new PointF(float32 rect.Left, float32 rect.Top); new PointF(float32 rect.Right, float32 rect.Top); new PointF(float32 rect.Right, float32 rect.Bottom); new PointF(float32 rect.Left, float32 rect.Bottom) |]
    v2w.TransformPoints(bbox)
    let path = new GraphicsPath()
    path.AddPolygon(bbox)
    new Region(path)

  // Scale an image 
  let ScaleBitmap(img:Image, newdim:Size) =
    let res = new Bitmap(newdim.Width, newdim.Height)
    let g = Graphics.FromImage(res)
    g.ScaleTransform(img.HorizontalResolution / res.HorizontalResolution, img.VerticalResolution / res.VerticalResolution)
    g.ScaleTransform(float32 newdim.Width / float32 img.Width, float32 newdim.Height / float32 img.Height)
    g.DrawImage(img, 0, 0)
    res

  // Convert the image in a cirlce image
  let CircleImage(img:Image, rad:int) =
    let image = ScaleBitmap(img, new Size(2*rad, 2*rad))
    let circle_image = new Bitmap(2*rad, 2*rad)
    let g = Graphics.FromImage(circle_image)
    g.SmoothingMode <- SmoothingMode.AntiAlias
    use brush = new TextureBrush(image)
    g.FillEllipse(brush, 0, 0, 2*rad, 2*rad)
    circle_image

  // Calculate the maximum space for new new radius
  let MaximumSpace(nodes:ResizeArray<Node>, newnode:Node) =
    let mutable maxradius = infinityf
    for node in nodes do
      maxradius <- min maxradius (newnode.Distance(node.Center) - node.Radius)
    maxradius

  // Stop all nodes and set the new resting lenght for the arcs
  let StopSystem(nodes:ResizeArray<Node>, arcs:ResizeArray<Arc>) =
    nodes |> Seq.iter(fun n -> n.V <- new Vector())
    arcs |> Seq.iter(fun a -> a.RestingLenght <- a.Lenght)

  // Return all the arcs connected to the node $n
  let ArcsConnectedNode(arcs:ResizeArray<Arc>, n:Node) =
    let arcs_connected = new ResizeArray<Arc>()
    for arc in arcs do
      if (arc.StartNode.IsSome &&  arc.EndNode.IsSome && (arc.StartNode.Value = n || arc.EndNode.Value = n)) then
        arcs_connected.Add(arc)
    arcs_connected

  // Calculate acceleration of node $n
  let HookeForce(arcs:ResizeArray<Arc>, n:Node) =
    // Sum of all the force
    let mutable f = new IUM.MidTerm.Vector()
    let connected_arcs = ArcsConnectedNode(arcs, n).ToArray()
    for arc in connected_arcs do
      let hf = arc.HookeForce(n)
      f <- f + hf
    f