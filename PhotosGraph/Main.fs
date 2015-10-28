#if INTERACTIVE
  #load "Buttons.fs";
  #load "Vector.fs";
  #load "Node.fs";
  #load "Arc.fs";
  #load "ImageCache.fs";
  #load "PhotosGraph_Functions.fs";
  #load "PhotosGraph.fs";;
#endif

open System.Windows.Forms
open System.Drawing

let f = new Form(Text = "Graph of Photos", TopMost = true, BackColor = Color.White, Width = 800, Height = 500)
f.StartPosition <- FormStartPosition.CenterScreen
f.Show()

let photos_graph = new IUM.MidTerm.PhotosGraph(Dock = DockStyle.Fill)
f.Controls.Add(photos_graph)
photos_graph.Focus() |> ignore 

// Application loop:
[<System.STAThread>]
while f.Created do
  Application.DoEvents()
  photos_graph.Animate()