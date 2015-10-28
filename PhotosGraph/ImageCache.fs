namespace IUM.MidTerm

open System.Drawing
open System.Drawing.Drawing2D
open IUM.MidTerm

type ImageCache(nodes:ResizeArray<Node>) =
  let cache = new System.Collections.Generic.Dictionary<Node, System.WeakReference>()
  let visible = new System.Collections.Generic.Dictionary<Node, Image>()

  member this.Size
    with get() = nodes.Count

  member this.GetImageBounds(n:Node) =
    n.Rectangle

  member this.GetImage(n) =
    if not(cache.ContainsKey(n)) then 
      cache.[n] <- new System.WeakReference(null)

    if not(cache.[n].IsAlive) then
      printfn "Loading image: %s of node %d ..." n.PathImage (nodes.IndexOf(n))
      cache.[n].Target <- Image.FromFile(n.PathImage)
    cache.[n].Target :?> Image
      
  member this.SetVisible(n) b =
    if b then
      if not(visible.ContainsKey(n)) then
        visible.Add(n, this.GetImage(n))
    else
      if visible.ContainsKey(n) then
        visible.Remove(n) |> ignore

  member this.ResetVisible() =
    visible.Clear()
