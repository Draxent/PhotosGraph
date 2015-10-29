# PhotosGraph
Graph of photos with elastic arcs
====

The system allows to create a graph composed of :
 - **Nodes:** You can create them using the left click. You can adjust the node's radius holding down the mouse and dragging it around. When you release the mouse, you need to choose an image for the node, otherwise it will be deleted. Once the procedure is complete, you can select the nodes and drag them in a new position or delete them pressing the key <b><i>Canc</i></b>.
 - **Arcs:** You can create them using the right click on one node and dragging onto another node. <br/>
 Then, you can select the arc and delete it pressing the key <b><i>Canc</i></b> or add a label to it pressing the key <b><i>T</i></b>. If the latter operation was performed, you can click the text area in order to edit the arc's label. It will appear a TextBox on the top-right corner of the screen. When you edit the text inside it, click any other point of the screen to see the change.

When the graph is complete and you are satisfied with the nodes' locations, you can start the animation pressing the key <b><i>PLAY</i></b> on the controller. <br/>
In this way, you change from the *"creative mode"* to the *"animation mode"*.

In this phase, all the arcs are considered like **uncompressed springs**. The only action allowed is to move around the nodes.
When you do it, you will compress or stretch the springs, so the system will react and you are able to see the beautiful game of fluctuations.The fluctuations are damped by a friction coefficient, that will cause the stop of the system after awhile, and changing automatically to the *"creative mode"*. The system will stop when the speed is imperceptible to the eye. 
In order to do that, I have chosen to calculate the average speed of a node (on a total of 100 animations) and compare it with a small value. If the averege is lower of this value, the system will stop.

During the animation, you can move the nodes several times. The speeds will be updated for every edit to the system.
At any time, you can press the key <b><i>STOP</i></b> on the controller in oreder to block the system.<br/>
Now, all the arcs will be considered again as uncompressed springs.

The images are managed with a cache system.<br/>
There are two dictionaries:

1. **cache:** it contains a list of WeakReference to the nodes' images. <br/>
The WeakReference are not protected by the Garbage Collection (GC), so if the system needs space, it can free them and reuse that memory. But, if the WeakReference is alive (i.e. GC did not claim that space back), there is no need to reload the image from scratch.
2. **visible:** it contains a list of images that we are viewing on the screen in a given moment.<br/>
Since we need them, this time they are StrongReference (i.e. normal variables protected by the GC).
	
In the *"Images"* folder, you can find some images ready to use.

The file <a href="https://github.com/Draxent/PhotosGraph/blob/master/usage_example.mp4">*usage_example.mp4*</a> shows an example of usage of the program.

##Keyboard
Besides the botton on the controller, *"the view"* that control the reference system in which we are, can be controlled by keyboard

| Key | Description |
|:-----:|:----------------------:|
| Enter | Start / Stop animation |
| A | Move view/node westward |
| W | Move view/node eastwards |
| D | Move view/node northwards |
| S | Move view/node southwards |
| Z | Zoom-in |
| X | Zoom-out |
| Q | Rotate counterclockwise |
| E | Rotate clockwise |

	
