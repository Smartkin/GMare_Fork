﻿#region MIT

// 
// GMare.
// Copyright (C) 2011, 2012, 2013, 2014 Michael Mercado
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

#endregion

using System;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;
using GMare.Objects;
using GMare.Forms;
using GMare.Graphics;

namespace GMare.Controls
{
    /// <summary>
    /// A control that edits various room elements
    /// </summary>
    public partial class GMareRoomPanel : Panel
    {
        #region Fields

        public event PositionHandler PositionChanged;                                // Mouse position changed event
        public delegate void PositionHandler();                                      // Mouse poisition changed event handler
        public event InstanceChangedHandler SelectedInstanceChanged;                 // Selected instance changed event
        public delegate void InstanceChangedHandler();                               // Selected instance changed event handler
        public event RoomChangingHandler RoomChanging;                               // Room changing event
        public delegate void RoomChangingHandler();                                  // Room changing event handler
        public event ClipboardChangedHandler ClipboardChanged;                       // Clipboard contents changed
        public delegate void ClipboardChangedHandler();                              // Clipboard contents changed handler
        private List<GMareInstance> _selectedInstances = new List<GMareInstance>();  // The currently selected instances
        private List<GMareInstance> _instanceClip = new List<GMareInstance>();       // The instance clipboard
        private Timer _stippleTimer = new Timer();                                   // Marching ants timer
        private GMareBackground _background = null;                                  // Selected background
        private GMareObject _selectedObject = null;                                  // The currently selected object
        private EditType _editMode = EditType.Layers;                                // The edit mode of the control
        private ToolType _toolMode = ToolType.Brush;                                 // The type of tool selected
        private GridType _gridMode = GridType.Normal;                                // The type of grid to draw
        private GMareBrush _brush = new GMareBrush();                                // The brush used for setting tile ids
        private GMareBrush _selection = null;                                        // A selection brush
        private GMareBrush _selectionClip = null;                                    // The selection clipboard
        private Cursor _cursorPencil = null;                                         // The pencil cursor
        private Cursor _cursorBucket = null;                                         // The bucket cursor
        private Cursor _cursorCross = null;                                          // The selection cursor
        private Cursor _cursorHandOpen = null;                                       // The viewport drag open hand
        private Cursor _cursorHandClose = null;                                      // The viewport drag closed hand
        private Point _mousePosition = Point.Empty;                                  // The position of the mouse within the control
        private Rectangle _instanceRectangle = Rectangle.Empty;                      // The instance selection rectangle
        private string _mouseActual = "-NA-";                                        // The actual position of the mouse
        private string _mouseSnapped = "-NA-";                                       // The snapped position of the mouse
        private string _mouseSector = "Tile: -NA-";                                  // The tile id, based on mouse position
        private string _mouseInstance = "-NA-";                                      // The instance, based on mous eposition
        private int _stippleOffset = 0;                                              // Selection rectangle stipple offset
        private int _depthIndex = 0;                                                 // The selected layer depth
        private int _layerIndex = -1;                                                // The selected layer index
        private int _gridX = 16;                                                     // Offset for grid width
        private int _gridY = 16;                                                     // Offset for grid height
        private int _posX = 0;                                                       // Last mouse x position
        private int _posY = 0;                                                       // Last mouse y position
        private int _level = 0;                                                      // The level of the collision
        private int _backgroundWidth = 0;                                            // Width of a condensed background image
        private bool _showGrid = true;                                               // If the grid should be displayed
        private bool _showCursor = false;                                            // If the cursor should be displayed
        private bool _showInstances = true;                                          // If instances should be displayed in layer edit mode
        private bool _showBlocks = true;                                             // If block instances should be displayed in layer edit mode
        private bool _dragging = false;                                              // If in a dragging operation
        private bool _moving = false;                                                // If in a moving operation
        private bool _moved = false;                                                 // If a selection has moved since it first was selected
        private bool _snap = true;                                                   // If the instances created should snap to the grid
        private bool _shiftKey = false;                                              // If the shift key is being held down
        private bool _controlKey = false;                                            // If the control key is being held down
        private bool _handKey = false;                                               // If the hand tool key is being held down
        private bool _mouseDown = false;                                             // If the mouse if being held down
        private bool _avoidMouseEvents = false;                                      // If avoiding the mouse events by dialog double click

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the selected background
        /// </summary>
        public GMareBackground SelectedBackground
        {
            get { return _background; }
            set { _background = value; }
        }

        /// <summary>
        /// Gets or sets the currently selected object
        /// </summary>
        public GMareObject SelectedObject
        {
            get { return _selectedObject; }
            set { _selectedObject = value; }
        }

        /// <summary>
        /// Gets or sets the currently selected instance
        /// </summary>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<GMareInstance> SelectedInstances
        {
            get { return _selectedInstances; }
            set { _selectedInstances = value; }
        }

        /// <summary>
        /// Gets the instance clipboard
        /// </summary>
        public List<GMareInstance> InstanceClipboard
        {
            get { return _instanceClip; }
        }

        /// <summary>
        /// Gets the selection clipboard
        /// </summary>
        public GMareBrush SelectionClipboard
        {
            get { return _selectionClip; }
        }

        /// <summary>
        /// Gets the current selection
        /// </summary>
        public GMareBrush Selection
        {
            get { return _selection; }
        }

        /// <summary>
        /// Gets or sets the selected layer
        /// </summary>
        public EditType EditMode
        {
            get { return _editMode; }
            set
            {
                _editMode = value;

                // If not editing layers, deselect
                if (_editMode != EditType.Layers)
                    mnuSelectionDeselect_Click(this, EventArgs.Empty);

                // Set the tool cursor
                SetCursor();

                // Force redraw
                Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets the current drawing tool type
        /// </summary>
        public ToolType ToolMode
        {
            get { return _toolMode; }
            set
            {
                // If not switching to the same selection type tool, deselect
                if (_toolMode == ToolType.Selection && value != ToolType.Selection)
                    mnuSelectionDeselect_Click(this, EventArgs.Empty);
                
                _toolMode = value;

                // Set the tool cursor
                SetCursor();
            }
        }

        /// <summary>
        /// Gets or sets the drawing grid mode
        /// </summary>
        public GridType GridMode
        {
            get { return _gridMode; }
            set { _gridMode = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets the tiles used to paint
        /// </summary>
        public GMareBrush Brush
        {
            get { return _brush; }
            set { _brush = value; }
        }

        /// <summary>
        /// The image to use as a texture
        /// </summary>
        public Bitmap Image
        {
            set
            {
                // Set the background width as per the condensed image
                _backgroundWidth = value == null ? 0 : value.Width;

                // Load texture into video RAM, delete image
                LoadTexture(value);
            }
        }

        /// <summary>
        /// Gets the actual mouse position
        /// </summary>
        public Point MouseLocation
        {
            get { return _mousePosition; }
        }

        /// <summary>
        /// Gets the actual mouse position
        /// </summary>
        public string MouseActual
        {
            get { return _mouseActual; }
        }

        /// <summary>
        /// Gets the sector the mouse is over
        /// </summary>
        public string MouseSector
        {
            get { return _mouseSector; }
        }

        /// <summary>
        /// Gets the current mouse snapped position
        /// </summary>
        public string MouseSnapped
        {
            get { return _mouseSnapped; }
        }

        /// <summary>
        /// Gets the current mouse over instance
        /// </summary>
        public string MouseInstance
        {
            get { return _mouseInstance; }
        }

        /// <summary>
        /// Sets the mouse's actual position
        /// </summary>
        private string SetMouseActual
        {
            set
            {
                _mouseActual = value;

                // Fire position changed event()
                if (PositionChanged != null)
                    PositionChanged();
            }
        }

        /// <summary>
        /// Sets the mouse's snapped position
        /// </summary>
        private string SetMouseSnapped
        {
            set
            {
                _mouseSnapped = value;

                // Fire position changed event()
                if (PositionChanged != null)
                    PositionChanged();
            }
        }

        /// <summary>
        /// Sets the sector the mouse is over
        /// </summary>
        private string SetMouseSector
        {
            set
            {
                _mouseSector = value;

                // Fire position changed event()
                if (PositionChanged != null)
                    PositionChanged();
            }
        }

        /// <summary>
        /// Set the mouse over instance
        /// </summary>
        private string SetMouseInstance
        {
            set
            {
                _mouseInstance = value;

                // Fire position changed event()
                if (PositionChanged != null)
                    PositionChanged();
            }
        }

        /// <summary>
        /// Gets or sets the scale factor of the room panel
        /// </summary>
        public float Zoom
        {
            get { return GraphicsManager.ScreenScale; }
            set { GraphicsManager.ScreenScale = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets the viewport offset
        /// </summary>
        public Point Offset
        {
            get { return new Point(GraphicsManager.OffsetX, GraphicsManager.OffsetY); }
            set { GraphicsManager.OffsetX = value.X; GraphicsManager.OffsetY = value.Y; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets the layer depth currently selected
        /// </summary>
        public int DepthIndex
        {
            get { return _depthIndex; }
            set { _depthIndex = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets the layer index currently selected
        /// </summary>
        public int LayerIndex
        {
            get { return _layerIndex; }
            set { _layerIndex = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets the horizontal grid spacing
        /// </summary>
        public int GridX
        {
            get { return _gridX; }
            set { _gridX = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets the vertical grid spacing
        /// </summary>
        public int GridY
        {
            get { return _gridY; }
            set { _gridY = value; Invalidate(); }
        }

        /// <summary>
        /// Gets the grid size
        /// </summary>
        private Size GridSize
        {
            get { return new Size(_gridX, _gridY); }
        }

        /// <summary>
        /// Gets or sets the level of the collsion
        /// </summary>
        public int Level
        {
            get { return _level; }
            set { _level = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets the show grid property
        /// </summary>
        public bool ShowGrid
        {
            get { return _showGrid; }
            set { _showGrid = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets the show instances always
        /// </summary>
        public bool ShowInstances
        {
            get { return _showInstances; }
            set { _showInstances = value; Invalidate(); }
        }

        /// <summary>
        /// Gets or sets whether to snap instances to the grid
        /// </summary>
        public bool Snap
        {
            get { return _snap; }
            set { _snap = value; }
        }

        /// <summary>
        /// Gets or sets whether the shift key is being held down
        /// </summary>
        public bool ShiftKey
        {
            get { return _shiftKey; }
            set { _shiftKey = value; }
        }

        /// <summary>
        /// Gets or sets whether the control key is being held down
        /// </summary>
        public bool ControlKey
        {
            get { return _controlKey; }
            set { _controlKey = value; }
        }

        /// <summary>
        /// Gets or sets whether the shift key is being held down
        /// </summary>
        public bool HandKey
        {
            get { return _handKey; }
            set
            {
                _handKey = value;

                // If the hand tool is being readied, use hand open cursor
                if (_handKey == true)
                    this.Cursor = _cursorHandOpen;
                else  // Set the cursoe normally
                    SetCursor();
                
                // Dragging the view, change cursor
                if (_mouseDown == true)
                    this.Cursor = _cursorHandClose;
            }
        }

        /// <summary>
        /// Gets or sets whether the block instances should be shown
        /// </summary>
        public bool ShowBlocks
        {
            get { return _showBlocks; }
            set
            {
                _showBlocks = value;

                // Remove all selected block instances
                _selectedInstances.RemoveAll(GMareInstance => GMareInstance.TileId != -1);

                // Trigger selected instance changed event
                if (SelectedInstanceChanged != null)
                    SelectedInstanceChanged();

                // Force redraw
                Invalidate();
            }
        }

        /// <summary>
        /// Gets or sets if mouse events should be ignored because of a dialog double click
        /// </summary>
        public bool AvoidMouseEvents
        { 
            get { return _avoidMouseEvents; }
            set
            {
                _avoidMouseEvents = value;
                
                Point p = this.PointToClient(Cursor.Position);
                _posX = p.X;
                _posY = p.Y;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Constructs a new room editor
        /// </summary>
        public GMareRoomPanel()
        {
            InitializeComponent();

            // For mouse wheel scrolling support
            this.SetStyle(ControlStyles.Selectable, true);

            // For resizing flicker issues
            this.SetStyle(ControlStyles.UserPaint, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.Opaque, true);

            // Set cursors
            _cursorPencil = new Cursor(GetType().Assembly.GetManifestResourceStream("GMare.Resources.cur_pencil.cur"));
            _cursorBucket = new Cursor(GetType().Assembly.GetManifestResourceStream("GMare.Resources.cur_bucket.cur"));
            _cursorCross = new Cursor(GetType().Assembly.GetManifestResourceStream("GMare.Resources.cur_cross.cur"));
            _cursorHandOpen = new Cursor(GetType().Assembly.GetManifestResourceStream("GMare.Resources.cur_hand_open.cur"));
            _cursorHandClose = new Cursor(GetType().Assembly.GetManifestResourceStream("GMare.Resources.cur_hand_close.cur"));

            // Set stipple timer
            _stippleTimer.Interval = 100;
            _stippleTimer.Tick += new EventHandler(Timer_Tick);
            _stippleTimer.Start();

            // Set brush options click events
            mnuBrushEdit.Click += new EventHandler(mnuBrushEdit_Click);
            mnuBrushColor.Click += new EventHandler(mnuBrushColor_Click);
            mnuBrushFlipHorizontal.Click += new EventHandler(mnuBrushFlipHorizontally_Click);
            mnuBrushFlipVertical.Click += new EventHandler(mnuBrushFlipVertically_Click);

            // Set selection options click events
            mnuSelectionCut.Click += new EventHandler(mnuSelectionCut_Click);
            mnuSelectionCopy.Click += new EventHandler(mnuSelectionCopy_Click);
            mnuSelectionPaste.Click += new EventHandler(mnuSelectionPaste_Click);
            mnuSelectionDeselect.Click += new EventHandler(mnuSelectionDeselect_Click);
            mnuSelectionDelete.Click += new EventHandler(mnuSelectionDelete_Click);
            mnuSelectionBrush.Click += new EventHandler(mnuSelectionBrush_Click);
            mnuSelectionAddBrush.Click += new EventHandler(mnuSelectionAdd_Click);
            mnuSelectionFlipHorizontal.Click += new EventHandler(mnuSelectionFlipX_Click);
            mnuSelectionFlipVertical.Click += new EventHandler(mnuSelectionFlipY_Click);
            mnuSelectionColor.Click += new EventHandler(mnuSelectionColor_Click);

            // Set instance options click events
            mnuInstanceReplace.Click += new EventHandler(mnuInstanceReplace_Click);
            mnuInstanceReplaceAll.Click += new EventHandler(mnuInstanceReplaceAll_Click);
            mnuInstanceCut.Click += new EventHandler(mnuInstanceCut_Click);
            mnuInstanceCopy.Click += new EventHandler(mnuInstanceCopy_Click);
            mnuInstancePaste.Click += new EventHandler(mnuInstancePaste_Click);
            mnuInstancePosition.Click += new EventHandler(mnuInstancePosition_Click);
            mnuInstanceSendBack.Click += new EventHandler(mnuInstanceSendBack_Click);
            mnuInstanceBringFront.Click += new EventHandler(mnuInstanceSendFront_Click);
            mnuInstanceSnap.Click += new EventHandler(mnuInstanceSnap_Click);
            mnuInstanceCode.Click += new EventHandler(mnuInstanceCode_Click);
            mnuInstanceDelete.Click += new EventHandler(mnuInstanceDelete_Click);
            mnuInstanceDeleteAll.Click += new EventHandler(mnuInstanceDeleteAll_Click);
            mnuInstanceClear.Click += new EventHandler(mnuInstanceClear_Click);
        }

        #endregion

        #region Events

        #region Brush Menu

        /// <summary>
        /// Create a brushes edit form
        /// </summary>
        private void mnuBrushEdit_Click(object sender, EventArgs e)
        {
            // If a room is not being edited, return
            if (CanDraw() == false)
                return;

            // Create a new array of brushes
            GMareBrush[] brushes = new GMareBrush[ProjectManager.Room.Brushes.Count];

            for (int i = 0; i < ProjectManager.Room.Brushes.Count; i++)
                brushes[i] = ProjectManager.Room.Brushes[i].Clone();

            // Create a new brushes edit
            using (EditBrushForm form = new EditBrushForm(brushes))
            {
                // If Ok was clicked.
                if (form.ShowDialog() == DialogResult.OK)
                {
                    ProjectManager.Room.Brushes.Clear();
                    ProjectManager.Room.Brushes.AddRange(form.Brushes);
                }
            }
        }

        /// <summary>
        /// Flip horizontally menu click
        /// </summary>
        public void mnuBrushFlipHorizontally_Click(object sender, EventArgs e)
        {
            // If the selection is empty, return
            if (_brush == null)
                return;

            // Show warning message
            if (ProjectManager.Room.Backgrounds[0].Image != null && ProjectManager.Room.ScaleWarning == true)
                ShowWarning(GMare.Properties.Resources.ScaleWarning);

            // Flip brush horizontally
            _brush.Flip(FlipDirectionType.Horizontal);

            // Force redraw
            Invalidate();
        }

        /// <summary>
        /// Flip vertically menu click
        /// </summary>
        public void mnuBrushFlipVertically_Click(object sender, EventArgs e)
        {
            // If the selection is empty, return
            if (_brush == null)
                return;

            // Show warning message
            if (_background.Image != null && ProjectManager.Room.ScaleWarning == true)
                ShowWarning(GMare.Properties.Resources.ScaleWarning);

            // Flip the brush vertically
            _brush.Flip(FlipDirectionType.Vertical);

            // Force redraw
            Invalidate();
        }

        /// <summary>
        /// Brush blend color menu click
        /// </summary>
        private void mnuBrushColor_Click(object sender, EventArgs e)
        {
            // If the selection is empty, return
            if (_brush == null)
                return;

            // Show warning message
            if (_background.Image != null && ProjectManager.Room.BlendWarning == true)
                ShowWarning(GMare.Properties.Resources.BlendWarning);

            // Create a color dialog
            using (ColorDialog form = new ColorDialog())
            {
                // Set user custom colors
                form.CustomColors = ProjectManager.Room.CustomColors;

                // If the dialog result is Ok
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // Set the color blend for each selection tile
                    foreach (GMareTile tile in _brush.Tiles)
                    {
                        // If the tile is not empty, set blend
                        if (tile.TileId != -1)
                            tile.Blend = form.Color;
                    }

                    // Set custom colors
                    ProjectManager.Room.CustomColors = form.CustomColors;

                    // Force redraw
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// Menu item click
        /// </summary>
        private void mnuBrushOptions_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            // If the tag holds a brush, set brush
            if (e.ClickedItem.Tag is GMareBrush)
                _brush = e.ClickedItem.Tag as GMareBrush;
        }

        #endregion

        #region Selection Menu

        /// <summary>
        /// Cut menu click
        /// </summary>
        public void mnuSelectionCut_Click(object sender, EventArgs e)
        {
            // If there is a selection
            if (_selection != null)
            {
                // Set clipboard
                _selectionClip = _selection.Clone();

                // If the selection has not moved, set tiles empty under selection
                if (_moved == false)
                {
                    // Room is about to change, record it
                    RoomChanging();
                    SetTiles(_selection.ToRectangle().X, _selection.ToRectangle().Y, true, _selection, true);
                }
            }

            // Empty the selection
            _selection = null;

            // Clipboard changed
            if (ClipboardChanged != null)
                ClipboardChanged();

            // Force redraw
            Invalidate();
        }

        /// <summary>
        /// Menu copy click
        /// </summary>
        public void mnuSelectionCopy_Click(object sender, EventArgs e)
        {
            // If there is a selection
            if (_selection != null)
            {
                // Set clipboard
                _selectionClip = _selection.Clone();

                // Room changed, selected tiles set
                if (_moved == true)
                    RoomChanging();

                // Set selection tiles to layer
                SetTiles(_selection.ToRectangle().X, _selection.ToRectangle().Y, false, _selection, true);
            }

            // Reset moved flag
            _moved = false;

            // Empty the selection
            _selection = null;

            // Clipboard changed
            if (ClipboardChanged != null)
                ClipboardChanged();

            // Force redraw
            Invalidate();
        }

        /// <summary>
        /// Paste menu click
        /// </summary>
        public void mnuSelectionPaste_Click(object sender, EventArgs e)
        {
            // If there was a previous selection
            if (_selection != null)
            {
                // Room is about to change, record it
                RoomChanging();
                SetTiles(_selection.StartX, _selection.StartY, false, _selection, true);
            }

            // Set the selection
            _selection = _selectionClip.Clone();

            // Clipboard changed
            ClipboardChanged();

            // Get the smallest size
            Size size = GetSmallestCanvas();

            // Get tile size
            Size tileSize = _background.TileSize;

            // Get centered position
            int x = (size.Width / 2) - (_selection.Width / 2);
            int y = (size.Height / 2) - (_selection.Height / 2);

            // Remember original size before transform
            int width = _selection.Width;
            int height = _selection.Height;

            // Get the center point snapped
            Point snap = GetTranslatedSnappedPoint(new Point(x, y), tileSize);

            // Set the new selection position
            _selection.StartX = snap.X;
            _selection.StartY = snap.Y;
            _selection.EndX = snap.X + width;
            _selection.EndY = snap.Y + height;

            // Set moved flag
            _moved = true;

            // Force redraw
            Invalidate();
        }

        /// <summary>
        /// Brush from selection menu click
        /// </summary>
        private void mnuSelectionBrush_Click(object sender, EventArgs e)
        {
            // If not in selection mode or the selection is empty, return
            if (_editMode != EditType.Layers || _toolMode != ToolType.Selection || _selection == null)
                return;

            // Set brush to selection
            _brush = _selection.Clone();
        }

        /// <summary>
        /// Add brush from selection menu click
        /// </summary>
        private void mnuSelectionAdd_Click(object sender, EventArgs e)
        {
            // If not in selection mode or the selection is empty or cannot draw, return
            if (_editMode != EditType.Layers || _toolMode != ToolType.Selection || _selection == null || ProjectManager.Room.Backgrounds[0] == null || CanDraw() == false)
                return;

            // Create a new brush form
            using (SaveBrushForm form = new SaveBrushForm(ProjectManager.Room.Backgrounds[0].GetCondensedTileset(), _selection, _background.TileSize))
            {
                // If the dialog result is Ok
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // Create a brush
                    GMareBrush brush = _selection.Clone();
                    brush.Name = form.BrushName;
                    brush.Glyph = form.BrushGlyph;

                    // Add the selection to the brushes list
                    ProjectManager.Room.Brushes.Add(brush);
                }
            }
        }

        /// <summary>
        /// Deselect click
        /// </summary>
        private void mnuSelectionDeselect_Click(object sender, EventArgs e)
        {
            // If selection moved and the selection is not empty
            if (_moved == true && _selection != null)
            {
                // Room is about to change, set tiles
                RoomChanging();
                SetTiles(_selection.ToRectangle().X, _selection.ToRectangle().Y, false, _selection, true);
            }

            // Reset moved flag
            _moved = false;

            // Empty the selection
            _selection = null;

            // Clipboard changed
            if (ClipboardChanged != null)
                ClipboardChanged();

            // Force redraw
            Invalidate();
        }

        /// <summary>
        /// Delete click
        /// </summary>
        public void mnuSelectionDelete_Click(object sender, EventArgs e)
        {
            // If the selection is not empty and the selection has not moved
            if (_selection != null && _moved == false)
            {
                // Room is about to change, set tiles empty
                RoomChanging();
                SetTiles(_selection.ToRectangle().X, _selection.ToRectangle().Y, true, _selection, true);
            }

            // Reset moved flag
            _moved = false;

            // Empty tile selection
            _selection = null;

            // Clipboard changed
            ClipboardChanged();

            // Force redraw
            Invalidate();
        }

        /// <summary>
        /// Flip horizontally click
        /// </summary>
        public void mnuSelectionFlipX_Click(object sender, EventArgs e)
        {
            // If the selection is empty, return
            if (_selection == null)
                return;

            // Show warning message
            if (ProjectManager.Room != null && ProjectManager.Room.ScaleWarning == true)
                ShowWarning(GMare.Properties.Resources.ScaleWarning);

            // Flip selection horizontally
            _selection.Flip(FlipDirectionType.Horizontal);

            // Set that the selection changed
            _moved = true;

            // Force redraw
            Invalidate();
        }

        /// <summary>
        /// Flip vertically click
        /// </summary>
        public void mnuSelectionFlipY_Click(object sender, EventArgs e)
        {
            // If the selection is empty, return
            if (_selection == null)
                return;

            // Show warning message
            if (ProjectManager.Room != null && ProjectManager.Room.ScaleWarning == true)
                ShowWarning(GMare.Properties.Resources.ScaleWarning);

            // Flip selection vertically
            _selection.Flip(FlipDirectionType.Vertical);

            // Set that the selection changed
            _moved = true;

            // Force redraw
            Invalidate();
        }

        /// <summary>
        /// Blend color click
        /// </summary>
        public void mnuSelectionColor_Click(object sender, EventArgs e)
        {
            // If the selection is empty, return
            if (_selection == null)
                return;

            // Show warning message
            if (ProjectManager.Room != null && ProjectManager.Room.BlendWarning == true)
                ShowWarning(GMare.Properties.Resources.BlendWarning);

            // Create a color dialog
            using (ColorDialog form = new ColorDialog())
            {
                // If Ok was clicked
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // Set the color blend for each selection tile
                    foreach (GMareTile tile in _selection.Tiles)
                    {
                        // If the tile is not empty, set blend
                        if (tile.TileId != -1)
                            tile.Blend = form.Color;
                    }

                    // Set that the selection changed
                    _moved = true;

                    // Force redraw
                    Invalidate();
                }
            }
        }

        #endregion

        #region Instance Menu

        /// <summary>
        /// Instance menu opening
        /// </summary>
        private void mnuInstanceOptions_Opening(object sender, CancelEventArgs e)
        {
            // Allow all options by default
            for (int i = 0; i < mnuInstanceOptions.Items.Count; i++)
                mnuInstanceOptions.Items[i].Enabled = true;

            for (int i = 0; i < mnuInstanceOptions.Items.Count; i++)
                mnuInstanceOptions.Items[i].Visible = true;

            // If instances were selected
            if (_selectedInstances.Count > 0)
            {
                // Make the delete all option not visible
                mnuInstanceDeleteAll.Visible = false;

                // If a single instance has been selected, enable replacement and position options
                if (_selectedInstances.Count == 1)
                {
                    // Allow position changing
                    mnuInstancePosition.Enabled = true;

                    // If an object has been selected
                    if (_selectedObject != null)
                    {
                        mnuInstanceReplace.Enabled = true;
                        mnuInstanceReplaceAll.Enabled = true;
                        mnuInstanceReplace.Text = "Replace With: " + _selectedObject.Resource.Name;
                        mnuInstanceReplaceAll.Text = "Replace All With: " + _selectedObject.Resource.Name;
                    }
                    else  // No object selected, hint to the user to select an object
                    {
                        mnuInstanceReplace.Enabled = false;
                        mnuInstanceReplaceAll.Enabled = false;
                        mnuInstanceReplace.Text = "Replace <undefined>";
                        mnuInstanceReplaceAll.Text = "Replace All <undefined>";
                    }
                }
                else  // Multi-selected instances, turn off replacement, and position options
                {
                    // Do not qllow position changing
                    mnuInstancePosition.Enabled = false;

                    // Do not allow replacement options
                    mnuInstanceReplace.Visible = false;
                    mnuInstanceReplaceAll.Visible = false;
                    mnuSeperator03.Visible = false;
                }

                // Set delete all text
                if (_selectedInstances.Count == 1)
                {
                    mnuInstanceDeleteAll.Visible = true;
                    mnuInstanceDeleteAll.Text = "Delete All: " + _selectedInstances[0].ToString();
                }

                // Get all non-block instances
                List<GMareInstance> instances = _selectedInstances.FindAll(i => i.TileId == -1);

                // If the selected instances are all blocks
                if (instances.Count == 0)
                {
                    // Options not available for blocks
                    mnuInstanceReplace.Visible = false;
                    mnuInstanceReplaceAll.Visible = false;
                    mnuInstanceCut.Visible = false;
                    mnuInstanceCopy.Visible = false;
                    mnuInstancePosition.Visible = false;
                    mnuInstanceSendBack.Visible = false;
                    mnuInstanceBringFront.Visible = false;
                    mnuInstanceSnap.Visible = false;
                    mnuInstanceDelete.Visible = false;
                    mnuInstanceDeleteAll.Visible = false;
                    mnuSeperator03.Visible = false;
                }
            }
            else  // Disable everything
            {
                for (int i = 0; i < mnuInstanceOptions.Items.Count; i++)
                    mnuInstanceOptions.Items[i].Visible = false;
            }

            // If there is something on the clipboard
            mnuInstancePaste.Enabled = _instanceClip.Count > 0 ? true : false;
            mnuInstancePaste.Visible = true;

            // Always allow instance clearing
            mnuInstanceClear.Visible = true;
            mnuInstanceClear.Enabled = true;
        }

        /// <summary>
        /// Instance replace menu click
        /// </summary>
        public void mnuInstanceReplace_Click(object sender, EventArgs e)
        {
            // If there is not only one instance or a block instance has been selected or no selected object, return
            if (_selectedInstances.Count != 1 || _selectedInstances[0].TileId != -1 || _selectedObject == null)
                return;

            // Room changing
            RoomChanging();

            // Replace the selected instance with the selected object resource
            _selectedInstances[0].ObjectId = _selectedObject.Resource.Id;
            _selectedInstances[0].Name = _selectedObject.Resource.Name;

            // Force redraw
            Invalidate();
        }

        /// <summary>
        /// Instance replace all menu click
        /// </summary>
        public void mnuInstanceReplaceAll_Click(object sender, EventArgs e)
        {
            // If there is not only one instance or a block instance has been selected or no selected object, return
            if (_selectedInstances.Count != 1 || _selectedInstances[0].TileId != -1 || _selectedObject == null)
                return;

            // Room changing
            RoomChanging();

            // Get target id
            int id = _selectedInstances[0].ObjectId;

            // Iterate through instances
            foreach (GMareInstance instance in ProjectManager.Room.Instances)
            {
                // If the instance matches the target object id
                if (instance.ObjectId == id)
                {
                    // Replace the selected instance with the selected object resource
                    instance.ObjectId = _selectedObject.Resource.Id;
                    instance.Name = _selectedObject.Resource.Name;
                }
            }

            // Force redraw
            Invalidate();
        }

        /// <summary>
        /// Instance cut menu click
        /// </summary>
        public void mnuInstanceCut_Click(object sender, EventArgs e)
        {
            // Get all non-block instances
            List<GMareInstance> instances = _selectedInstances.FindAll(i => i.TileId == -1);
            
            // If no non-block instances have been selected, return
            if (instances == null)
                return;

            // Clear the instance clip
            _instanceClip.Clear();

            // Room changed, instance deleted
            RoomChanging();

            // Set instance clipboard
            foreach (GMareInstance instance in _selectedInstances)
            {
                // If not a block instance, cut
                if (instance.TileId == -1)
                {
                    // Copy instance to the clipboard
                    _instanceClip.Add(instance.Clone());

                    // Remove the instance from the room
                    ProjectManager.Room.Instances.Remove(instance);
                }
            }

            // Set selected instance
            SetSelectedInstance(null, false);
        }

        /// <summary>
        /// Instance copy menu click
        /// </summary>
        public void mnuInstanceCopy_Click(object sender, EventArgs e)
        {
            // Get all non-block instances
            List<GMareInstance> instances = _selectedInstances.FindAll(i => i.TileId == -1);

            // If no non-block instances have been selected, return
            if (instances == null)
                return;

            // Clear the instance clip
            _instanceClip.Clear();

            // Copy instances to the clipboard if not a block instance
            foreach (GMareInstance instance in _selectedInstances)
                if (instance.TileId == -1)
                    _instanceClip.Add(instance.Clone());

            // Clipboard changed
            ClipboardChanged();
        }

        /// <summary>
        /// Instance paste menu click
        /// </summary>
        public void mnuInstancePaste_Click(object sender, EventArgs e)
        {
            // If the instance clip is empty, return
            if (_instanceClip.Count == 0)
                return;

            // Room changing, instances being pasted
            RoomChanging();

            // Get the smallest canvas size
            Size canvas = GetSmallestCanvas();

            // New instances that will be pasted
            List<GMareInstance> instances = new List<GMareInstance>();

            // Get the bounding rectangle for instances on the clipboard we are copying
            Rectangle selection = GetInstanceRectangle(_instanceClip);
            
            // Get the centered position of the selection vs. the screen
            Point position = new Point();
            position.X = ((int)(canvas.Width) / 2) - ((int)(selection.Width) / 2);
            position.Y = ((int)(canvas.Height) / 2) - ((int)(selection.Height) / 2);

            // Paste copies of the instances on the clipboard
            foreach (GMareInstance inst in _instanceClip)
            {
                // Get object the instance represents
                GMareObject obj = ProjectManager.Room.Objects.Find(o => o.Resource.Id == inst.ObjectId);

                // If no object was found, return
                if (obj == null)
                    continue;

                // Get instance offset from original origin to pasting origin
                Point offset = new Point((inst.X > selection.X ? inst.X - selection.X : selection.X - inst.X), 
                                         (inst.Y > selection.Y ? inst.Y - selection.Y : selection.Y - inst.Y));

                // Calculate instance position
                int x = position.X + (int)(offset.X * Zoom);
                int y = position.Y + (int)(offset.Y * Zoom);
                
                // Create pasting instance point
                Point location = _snap ? GetTranslatedSnappedPoint(new Point(x, y), GridSize) : GetTranslatedPoint(new Point(x, y));

                // Clone the instance
                GMareInstance instance = inst.Clone();

                // Set centered position
                instance.X = location.X;
                instance.Y = location.Y;

                // Add the new instance
                ProjectManager.Room.Instances.Add(instance);

                // Set selected instance
                instances.Add(instance);
            }

            // Set selected instances
            SetSelectedInstances(instances);
        }

        /// <summary>
        /// Instance position change click
        /// </summary>
        public void mnuInstancePosition_Click(object sender, EventArgs e)
        {
            // If one instance has not been selected, return
            if (_selectedInstances.Count != 1)
                return;

            // Create a new position change form
            using (PositionForm form = new PositionForm(_selectedInstances[0].X, _selectedInstances[0].Y))
            {
                // If the dialog result is Ok
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // If no change in position, return
                    if (_selectedInstances[0].X == form.Position.X && _selectedInstances[0].Y == form.Position.Y)
                        return;

                    // Element of the room changing
                    RoomChanging();

                    // Set new position
                    _selectedInstances[0].X = _snap ? (form.Position.X / _gridX) * _gridX : form.Position.X;
                    _selectedInstances[0].Y = _snap ? (form.Position.Y / _gridY) * _gridY : form.Position.Y;

                    // Update
                    SetSelectedInstance(_selectedInstances[0], false);
                }
            }
        }

        /// <summary>
        /// Instance send front menu click
        /// </summary>
        public void mnuInstanceSendFront_Click(object sender, EventArgs e)
        {
            // If the selected instance is empty, return
            if (_selectedInstances.Count == 0)
                return;

            // Room changed, instance index changed
            RoomChanging();

            // Selected instances list
            List<GMareInstance> selected = new List<GMareInstance>();

            // Send the selected instance to the end of the list
            foreach (GMareInstance instance in _selectedInstances)
            {
                GMareInstance inst = instance.Clone();
                selected.Add(inst);

                ProjectManager.Room.Instances.Add(inst);
                ProjectManager.Room.Instances.Remove(instance);
            }

            // Set selected instances
            SetSelectedInstances(selected);
        }

        /// <summary>
        /// Instance send back menu click
        /// </summary>
        public void mnuInstanceSendBack_Click(object sender, EventArgs e)
        {
            // If the selected instance is empty, return
            if (_selectedInstances == null)
                return;

            // Room changed, instance index changed
            RoomChanging();

            // Selected instances list
            List<GMareInstance> selected = new List<GMareInstance>();

            // Send the selected instance to the end of the list
            foreach (GMareInstance instance in _selectedInstances)
            {
                GMareInstance inst = instance.Clone();
                selected.Add(inst);

                ProjectManager.Room.Instances.Insert(0, inst);
                ProjectManager.Room.Instances.Remove(instance);
            }

            // Set selected instances
            SetSelectedInstances(selected);
        }

        /// <summary>
        /// Instance snap menu item
        /// </summary>
        public void mnuInstanceSnap_Click(object sender, EventArgs e)
        {
            // Get all non-block instances
            List<GMareInstance> instances = _selectedInstances.FindAll(i => i.TileId == -1);

            // If no non-block instances have been selected, return
            if (instances == null)
                return;

            // The new snapped point to move the instance to
            Point snap = Point.Empty;

            // Get the width and height of the snapped point
            int width = (int)(_gridX * Zoom);
            int height = (int)(_gridY * Zoom);

            // Calculate snapped point
            for (int i = 0; i < _selectedInstances.Count; i++)
            {
                // If a block instance, skip
                if (_selectedInstances[i].TileId != -1)
                    continue;

                // Get snap values
                snap.X = (int)((((_selectedInstances[i].X) / width) * width) / Zoom);
                snap.Y = (int)((((_selectedInstances[i].Y) / height) * height) / Zoom);

                // If no change was made, return
                if (_selectedInstances[i].X == snap.X && _selectedInstances[i].Y == snap.Y)
                    return;

                // Room changed, instance positions changed
                if (i == 0)
                    RoomChanging();

                // Set instance new position
                _selectedInstances[i].X = snap.X;
                _selectedInstances[i].Y = snap.Y;
            }

            // The selected instances changed
            SelectedInstanceChanged();
        }

        /// <summary>
        /// Instance creation code menu click
        /// </summary>
        public void mnuInstanceCode_Click(object sender, EventArgs e)
        {
            // If no instance has been selected, return
            if (_selectedInstances.Count == 0)
                return;

            // Code string
            string code = "";

            // Create a new script form
            if (_selectedInstances.Count == 1)
                code = (string)_selectedInstances[0].CreationCode.Clone();

            // Create a new script form
            using (ScriptForm form = new ScriptForm(code, "Creation Code"))
            {
                // If the dialog result is Ok
                if (form.ShowDialog() == DialogResult.OK)
                {
                    // Check if any code has been changed
                    List<GMareInstance> instances = _selectedInstances.FindAll(i => i.CreationCode != form.Code);

                    // If no change from the previous code, return
                    if (instances == null)
                        return;

                    // Room changed, instance creation code changed
                    RoomChanging();

                    // Set creation code
                    foreach (GMareInstance instance in _selectedInstances)
                        instance.CreationCode = (string)form.Code.Clone();

                    // The selected instance changed
                    SelectedInstanceChanged();
                }
            }
        }

        /// <summary>
        /// Instance delete menu click
        /// </summary>
        public void mnuInstanceDelete_Click(object sender, EventArgs e)
        {
            // Get all non-block instances
            List<GMareInstance> instances = _selectedInstances.FindAll(i => i.TileId == -1);

            // If nothing but block instances have been selected, return
            if (instances.Count == 0)
                return;

            // Room changed, instances deleted
            RoomChanging();

            // Remove selected instance(s)
            foreach (GMareInstance instance in instances)
                ProjectManager.Room.Instances.Remove(instance);

            // Update selection
            int index = -1;

            // Iterate through project instances
            for (int i = ProjectManager.Room.Instances.Count - 1; i > -1; i--)
            {
                // If not showing block instances and the instance is a block instance, continue
                if (!_showBlocks && ProjectManager.Room.Instances[i].TileId != -1)
                    continue;
                else
                {
                    // A valid selection
                    index = i;
                    break;
                }
            }

            // Set selected instance
            SetSelectedInstance(index == -1 ? null : ProjectManager.Room.Instances[index], false);
        }

        /// <summary>
        /// Instance delete all menu click
        /// </summary>
        public void mnuInstanceDeleteAll_Click(object sender, EventArgs e)
        {
            // Get all non-block instances
            List<GMareInstance> instances = _selectedInstances.FindAll(i => i.TileId == -1);

            // If no non-block instances have been selected, return
            if (instances == null)
                return;

            // Ask if the user really wants to delete all instances of a certain type
            DialogResult result = MessageBox.Show("Are you sure you want to delete all " + _selectedInstances[0].Name + " ?", "GMare", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

            // If the user wants to delete all instances of a certain type
            if (result == DialogResult.Yes)
            {
                // Room changed, all instances of set type deleted
                RoomChanging();

                // Delete all of the selected object type
                ProjectManager.Room.Instances.RemoveAll(i => i.ObjectId == _selectedInstances[0].ObjectId);

                // Update selection
                int index = -1;

                // Iterate through project instances
                for (int i = ProjectManager.Room.Instances.Count - 1; i > -1; i--)
                {
                    // If not showing block instances and the instance is a block instance, continue
                    if (!_showBlocks && ProjectManager.Room.Instances[i].TileId != -1)
                        continue;
                    else
                    {
                        // A valid selection
                        index = i;
                        break;
                    }
                }

                // Update
                SetSelectedInstance(null, false);
            }
        }

        /// <summary>
        /// Instance clear menu click
        /// </summary>
        public void mnuInstanceClear_Click(object sender, EventArgs e)
        {
            // If there is nothing to clear, return
            if (ProjectManager.Room.Instances.Count == 0)
                return;

            // Ask if the user if they really wants to clear all the instances
            DialogResult result = MessageBox.Show("Are you sure you want to clear all instances from the room? Note: This will not affect block instances.", "GMare", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

            // If the user wants to clear all instances
            if (result == DialogResult.Yes)
            {
                // Room changed, all instances deleted
                RoomChanging();

                // Clear all normal instances
                ProjectManager.Room.Instances.RemoveAll(i => i.TileId == -1);

                // Update
                SetSelectedInstance(null, false);
            }
        }

        #endregion

        #endregion

        #region Overrides

        #region Create

        /// <summary>
        /// On create control
        /// </summary>
        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            // If not in design mode, initialize OpenGL
            if (DesignMode == false)
                GraphicsManager.Initialize(this);

            // Set blend mode to consider image alpha data
            GraphicsManager.BlendMode = GraphicsManager.BlendType.Alpha;
        }

        #endregion

        #region Paint

        /// <summary>
        /// On paint
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            // If the control cannot draw room, return
            if (!CanDraw())
            {
                e.Graphics.Clear(this.BackColor);
                return;
            }

            // Clear the screen
            GraphicsManager.DrawClear(this.BackColor);

            // Begin drawing the scene
            GraphicsManager.BeginScene();
            int width = ProjectManager.Room.Width;
            int height = ProjectManager.Room.Height;

            // Draw a blank background for room
            GraphicsManager.DrawRectangle(new Rectangle(0, 0, width + 1, height + 1), ProjectManager.Room.BackColor, false);

            // Set scissor rectangle, to clip needless rendering
            Size size = GetSmallestCanvas();
            GraphicsManager.Scissor = new Rectangle(0, this.ClientSize.Height - size.Height, size.Width, size.Height);

            // Draw tiles
            if (_background.Image != null)
                DrawTiles();

            // If in object edit mode
            DrawInstances();

            // Draw grid
            DrawGrid();

            // Draw selection
            if (_editMode == EditType.Layers)
            {
                // Do action based on tool type
                switch (_toolMode)
                {
                    case ToolType.Brush: DrawBrush(); break;
                    case ToolType.Bucket: DrawBrush(); break;
                    case ToolType.Selection: DrawSelection(); break;
                }
            }

            // If in object editing mode, draw the selection rect, if avaialble
            if (_editMode == EditType.Objects)
                DrawInstanceSelection();

            // Disable scissor testing
            OpenGL.glDisable(GLOption.ScissorTest);

            // End drawing the scene
            GraphicsManager.EndScene();
        }

        /// <summary>
        /// On paint background
        /// </summary>
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Do nothing
        }

        #endregion

        #region Mouse

        /// <summary>
        /// On mouse down
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // If a room has not been loaded or a dialog mouse events, return
            if (!CanDraw() || _avoidMouseEvents)
                return;

            // If the hand key is being held down, return
            if (_handKey)
            {
                _mouseDown = true;
                return;
            }

            // Do action based on edit mode
            switch (_editMode)
            {
                case EditType.Layers: LayersMouseDown(e); break;
                case EditType.Objects: InstancesMouseDown(e); break;
            }
        }

        /// <summary>
        /// /// <summary>
        /// Mouse move in graphics panel
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            // If a room has not been loaded or a dialog mouse events, return
            if (!CanDraw() || _avoidMouseEvents)
            {
                _avoidMouseEvents = false;
                return;
            }

            // Calculate tile position
            Size tileSize = _background == null ? new Size(_gridX, _gridY) : _background.TileSize;
            Point snap = GetTranslatedSnappedPoint(e.Location, tileSize);

            // Set mouse position information
            _mousePosition = GetActualPoint(e.X, e.Y);
            this.SetMouseActual = "Actual: " + GetActualPoint(e.X, e.Y).ToString();
            this.SetMouseSnapped = "Snapped: " + snap.ToString();

            // If the hand key is being held down, return
            if (_handKey == true)
                return;

            // Do action based on edit mode
            switch (_editMode)
            {
                case EditType.Layers: LayersMouseMove(e); break;
                case EditType.Objects: InstancesMouseMove(e); break;
            }
        }

        /// <summary>
        /// Mouse up in graphics panel
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            // If a room has not been loaded or a dialog mouse events, return
            if (!CanDraw() || _avoidMouseEvents)
                return;

            // No more hand tool
            _mouseDown = false;

            // Do action based on edit mode
            switch (_editMode)
            {
                case EditType.Layers: LayersMouseUp(); break;
                case EditType.Objects: InstancesMouseUp(); break;
            }

            // Finish any dragging operation
            _dragging = false;
            base.OnMouseUp(e);
        }

        /// <summary>
        /// Mouse enter in graphics panel
        /// </summary>
        protected override void  OnMouseEnter(EventArgs e)
        {
            // Allow hooking of this event
            base.OnMouseEnter(e);

            // Set cursor
            SetCursor();

            // Allow showing the cursor
            _showCursor = true;

            // Update drawing
            Invalidate();
        }

        /// <summary>
        /// Mouse leave in graphics panel
        /// </summary>
        protected override void  OnMouseLeave(EventArgs e)
        {
            // Set mouse information
            SetMouseActual = "Actual: -NA-";
            SetMouseSnapped = "Snapped: -NA-";
            SetMouseSector = "Tile: -NA-";

            // Set cursor
            this.Cursor = Cursors.Arrow;

            // Do not allow showing the cursor
            _showCursor = false;

            // Update drawing
            Invalidate();
        }

        #endregion

        #endregion

        #region Methods

        #region Draw

        #region Draw Tiles

        /// <summary>
        /// Draws all the tiles in the tile array.
        /// </summary>
        private void DrawTiles()
        {
            // Get various variables.
            Size tileSize = _background.TileSize;
            int layers = ProjectManager.Room.Layers.Count - 1;
            int cols = ProjectManager.Room.Columns;
            int rows = ProjectManager.Room.Rows;
            int depth = 0;

            // Destination rectangle
            Rectangle position = new Rectangle(0, 0, tileSize.Width, tileSize.Height);

            // Source Rectangle
            Point source = Point.Empty;

            // Calculate tileset width
            int width = (int)Math.Floor((double)(_background.Image.Width - _background.OffsetX) / (double)(_background.TileWidth + _background.SeparationX)) * _background.TileWidth;

            // Iterate through layers
            for (int layer = layers; layer > -1; layer--)
            {
                // If the layer is not visible, skip
                if (ProjectManager.Room.Layers[layer].Visible == false)
                    continue;

                // Get layer depth
                depth = ProjectManager.Room.Layers[layer].Depth;

                // Set the blend mode based on drawing depth
                int index = GetIndex(depth);

                // FOR CHRIST SAKES CHANGE THIS
                // Iterate through columns
                for (int col = 0; col < cols; col++)
                {
                    // Iterate through rows
                    for (int row = 0; row < rows; row++)
                    {
                        // Get tile id
                        GMareTile tile = ProjectManager.Room.Layers[layer].Tiles[col, row];
                        int tileId = tile.TileId;

                        // If the tile is empty, continue looping
                        if (tileId == -1)
                            continue;

                        // Calculate destination rectangle
                        position.X = col * tileSize.Width;
                        position.Y = row * tileSize.Height;

                        Rectangle viewport = ClientRectangle;
                        viewport.X = Offset.X;
                        viewport.Y = Offset.Y;
                        viewport.Width = (int)(viewport.Width / Zoom);
                        viewport.Height = (int)(viewport.Height / Zoom);

                        // If the tile is visible within the viewport, draw tile
                        if (viewport.IntersectsWith(position) == false)
                            continue;

                        // Calculate source point
                        source = GMareBrush.TileIdToSector(tileId, width, tileSize);

                        // Scaling values
                        PointF scale = tile.GetScale();

                        // Draw tile to cache
                        if (source.X < GraphicsManager.TileMaps[0].GetLength(0) && source.Y < GraphicsManager.TileMaps[0].GetLength(1))
                            GraphicsManager.DrawTile(GraphicsManager.TileMaps[index][source.X, source.Y], position.X, position.Y, scale.X, scale.Y, 0, tile.Blend);
                    }
                }

                // Draw cache
                GraphicsManager.DrawSpriteBatch(true);
            }
        }

        /// <summary>
        /// Sets the blend mode for tile drawing.
        /// </summary>
        /// <param name="depth">The depth of the tile.</param>
        /// <returns>Tileset index to use for rendering.</returns>
        private int GetIndex(int depth)
        {
            // Do action based on edit mode.
            switch (_editMode)
            {
                // Draw dark for instance mode.
                case EditType.Objects:
                    return 1;

                // Layer mode.
                case EditType.Layers:

                    // Set tileset based on depth
                    if (depth == _depthIndex)
                        return 0;
                    else if (depth > _depthIndex)
                        return 1;
                    else if (depth < _depthIndex)
                        return 2;

                    break; 
            }

            // Draw normal as default.
            return 0;
        }

        #endregion

        #region Draw Grid

        /// <summary>
        /// Draws the grid
        /// </summary>
        private void DrawGrid()
        {
            // If the grid is not being shown, return
            if (_showGrid == false)
                return;

            // Position variables
            int x1 = 0;
            int y1 = 0;
            int x2 = 0;
            int y2 = 0;
            Size canvas = GetSmallestCanvas();

            // Calculate line amounts
            int cols = (int)((float)canvas.Width / (float)_gridX / Zoom) + 2;
            int rows = (int)((float)canvas.Height / (float)_gridY / Zoom) + 2;

            // Calculate offsets
            int offsetX = Offset.X % ProjectManager.Room.Width;
            int offsetY = Offset.Y % ProjectManager.Room.Height;

            // Calculate snap
            Point snap = GetTranslatedSnappedPoint(new Point(Offset.X - offsetX, Offset.Y - offsetY), new Size(_gridX, _gridY));

            // Grid color
            Color color = Color.FromArgb(128, Color.Black);

            // Draw grid based on grid mode
            switch (_gridMode)
            {
                // Draw a normal grid
                case GridType.Normal:

                    // Draw vertical lines
                    for (int col = 0; col < cols; col++)
                    {
                        // Calculate coordinates.
                        x1 = col * _gridX + snap.X;
                        y1 = snap.Y;
                        x2 = col * _gridX + snap.X;
                        y2 = (int)(canvas.Height / Zoom) + snap.Y + _gridY;

                        // Draw line
                        GraphicsManager.DrawLineCache(x1, y1, x2, y2, color);
                    }

                    // Draw horizontal lines
                    for (int row = 0; row < rows; row++)
                    {
                        // Calculate coordinates
                        x1 = snap.X;
                        y1 = row * _gridY + snap.Y;
                        x2 = (int)(canvas.Width / Zoom) + snap.X + _gridX;
                        y2 = row * _gridY + snap.Y;

                        // Draw line
                        GraphicsManager.DrawLineCache(x1, y1, x2, y2, color);
                    }

                    break;

                // Draw an isometric grid
                case GridType.Isometric:

                    // Iterate through visible rows
                    for (int y = MathMethods.DivideTowardsNegative(0, _gridY) * _gridY; y < rows * _gridY; y += _gridY)
                    {
                        // Iterate through visible columns
                        for (int x = MathMethods.DivideTowardsNegative(0, _gridX) * _gridX; x < cols * _gridX; x += _gridX)
                        {
                            // Calculate positions.
                            x1 = (x + (_gridX >> 1)) + snap.X;
                            y1 = (y + (_gridY >> 1)) + snap.Y;
                            x2 = (x + (_gridX + 1 >> 1)) + snap.X;
                            y2 = (y + (_gridY + 1 >> 1)) + snap.Y;

                            // Draw lines.
                            GraphicsManager.DrawLineCache(x + snap.X, y2, x1, y + _gridY + snap.Y, color);
                            GraphicsManager.DrawLineCache(x2, y + _gridY + snap.Y, x + _gridX + snap.X, y2, color);
                            GraphicsManager.DrawLineCache(x + _gridX + snap.X, y1, x2, y + snap.Y, color);
                            GraphicsManager.DrawLineCache(x1, y + snap.Y, x + snap.X, y1, color);
                        }
                    }

                    break;
            }

            // Draw line batch
            GraphicsManager.DrawStippledLineBatch(0, _gridMode == GridType.Normal ? 2 : 1);
        }

        #endregion

        #region Draw Brush

        /// <summary>
        /// Draws the selected tiles with a rectangle border.
        /// </summary>
        private void DrawBrush()
        {
            // If the cursor should not be drawn, return.
            if (_showCursor == false || _handKey == true || _background == null || _brush == null)
                return;

            // Get room tilesize.
            Size tileSize = _background.TileSize;

            // Create a new selection rectangle
            Rectangle selection = new Rectangle();

            // Get selection rectangle.
            selection = _brush.ToRectangle();
            selection.X = _posX;
            selection.Y = _posY;

            // Source Rectangle.
            Point source = Point.Empty;

            // Destination point.
            Point position = Point.Empty;

            // Iterate through tiles horizontally.
            for (int col = 0; col < _brush.Columns; col++)
            {
                // Iterate through tiles vertically.
                for (int row = 0; row < _brush.Rows; row++)
                {
                    // Calculate source point.
                    source = GMareBrush.TileIdToSector(_brush.Tiles[col, row].TileId, _backgroundWidth, tileSize);
                    position.X = _posX + col * tileSize.Width;
                    position.Y = _posY + row * tileSize.Height;

                    // If within bounds, draw tile.
                    if (source.X > -1 && source.X < GraphicsManager.TileMaps[2].GetLength(0) && source.Y > -1 && source.Y < GraphicsManager.TileMaps[2].GetLength(1))
                        GraphicsManager.DrawTile(GraphicsManager.TileMaps[2][source.X, source.Y], position.X, position.Y, _brush.Tiles[col, row].GetScale().X, _brush.Tiles[col, row].GetScale().Y, 0, _brush.Tiles[col, row].Blend);
                }
            }

            // Draw cache.
            GraphicsManager.DrawSpriteBatch(true);

            // Draw cursor border.
            selection.Width += 1;
            selection.Height += 1;
            GraphicsManager.DrawRectangle(selection, Color.Black, true);
            selection.X += 1;
            selection.Y += 1;
            selection.Width -= 2;
            selection.Height -= 2;
            GraphicsManager.DrawRectangle(selection, Color.White, true);
            selection.X += 1;
            selection.Y += 1;
            selection.Width -= 2;
            selection.Height -= 2;
            GraphicsManager.DrawRectangle(selection, Color.Black, true);
        }

        #endregion

        #region Draw Instances

        /// <summary>
        /// Draws all the instances within the room
        /// </summary>
        private void DrawInstances()
        {
            // If in layer edit mode and not showing instances and blocks, return
            if (_editMode == EditType.Layers && !_showInstances && !_showBlocks)
                return;

            // Iterate through room instances
            foreach (GMareInstance instance in ProjectManager.Room.Instances)
            {
                // If a selected instance and in object edit mode, it will be drawn elsewhere
                if (_selectedInstances.Contains(instance) == true && _editMode == EditType.Objects)
                    continue;

                // Do action based on edit mode
                switch (_editMode)
                {
                    case EditType.Layers:
                        if (_showBlocks && instance.TileId > -1)
                            GraphicsManager.DrawSpriteCached(instance.ObjectId, instance.X, instance.Y, Color.FromArgb(128, Color.White));
                        else if (_showInstances && instance.TileId == -1)
                            GraphicsManager.DrawSpriteCached(instance.ObjectId, instance.X, instance.Y, Color.FromArgb(128, Color.White));
                        break;

                    case EditType.Objects:
                        if (_showBlocks && instance.TileId > -1)
                            GraphicsManager.DrawSpriteCached(instance.ObjectId, instance.X, instance.Y, Color.FromArgb(128, Color.White));
                        else if (instance.TileId == -1)
                            GraphicsManager.DrawSpriteCached(instance.ObjectId, instance.X, instance.Y, Color.White);
                        break;
                }
            }

            // If placing a new instance, if there's no instances selected, and there is an object selected, draw new instance
            if (_dragging == true && _selectedInstances.Count == 0 && _selectedObject != null)
                    GraphicsManager.DrawSpriteCached(_selectedObject.Resource.Id, _posX - _selectedObject.OriginX, _posY - _selectedObject.OriginY, Color.FromArgb(128, Color.White));

            // Draw sprite batch
            GraphicsManager.DrawSpriteBatch(false);

            // If instances have been selected, and in object edit mode
            if (_selectedInstances.Count > 0 && _editMode == EditType.Objects)
            {
                // Iterate through selected instances
                foreach (GMareInstance instance in _selectedInstances)
                {
                    GraphicsManager.BlendMode = GraphicsManager.BlendType.Invert;
                    GraphicsManager.DrawSprite(instance.ObjectId, instance.X, instance.Y, Color.White);
                    GraphicsManager.BlendMode = GraphicsManager.BlendType.Alpha;
                }
            }
        }

        #endregion

        #region Draw Selection

        /// <summary>
        /// Draws selection from selection tool
        /// </summary>
        private void DrawSelection()
        {
            // If the selection is empty, return
            if (_selection == null || _background.Image == null)
                return;

            // Get room tilesize
            Size tileSize = _background.TileSize;

            // Calculate tileset width
            int width = _background.Image.Width;
            width = (width - ((width / tileSize.Width) * _background.SeparationX));

            // Source Rectangle
            Point source = Point.Empty;

            // Destination point
            Point position = Point.Empty;

            // Iterate through tiles horizontally
            for (int col = 0; col < _selection.Columns; col++)
            {
                // Iterate through tiles vertically
                for (int row = 0; row < _selection.Rows; row++)
                {
                    // Calculate source point
                    if (_selection.Tiles[col, row].TileId == -1)
                        continue;

                    // Calculate source point
                    source = GMareBrush.TileIdToSector(_selection.Tiles[col, row].TileId, width, tileSize);
                    position.X = _selection.ToRectangle().X + col * tileSize.Width;
                    position.Y = _selection.ToRectangle().Y + row * tileSize.Height;

                    // Scaling values
                    PointF scale = _selection.Tiles[col, row].GetScale();

                    // Draw tile
                    GraphicsManager.DrawTile(GraphicsManager.TileMaps[0][source.X, source.Y], position.X, position.Y, scale.X, scale.Y, 0, _selection.Tiles[col, row].Blend);
                }
            }

            // Draw sprite cache
            GraphicsManager.DrawSpriteBatch(true);

            // Create a selection rectangle
            Rectangle rect = _selection.ToRectangle();
            rect.Width += 1;
            rect.Height += 1;

            GraphicsManager.DrawRectangle(rect, Color.Black, true);
            GraphicsManager.DrawStippledRectangle(rect, Color.White, _stippleOffset, 1);
        }

        /// <summary>
        /// Draws an instance selection rectangle
        /// </summary>
        private void DrawInstanceSelection()
        {
            // If the instance selection rectangle is empty, return
            if (_instanceRectangle == Rectangle.Empty)
                return;

            // Draw selection rectangle
            GraphicsManager.DrawRectangle(_instanceRectangle, Color.FromArgb(128, 113, 170, 225), false);
            GraphicsManager.DrawRectangle(_instanceRectangle, Color.FromArgb(51, 153, 255), true);
        }

        #endregion

        #endregion

        #region Cursor

        /// <summary>
        /// Sets the room's cursor.
        /// </summary>
        public void SetCursor()
        {
            // Default cursor.
            this.Cursor = Cursors.Arrow;

            // If in layer edit mode.
            if (_editMode == EditType.Layers)
            {
                // Switch cursor based on tool mode.
                switch (_toolMode)
                {
                    case ToolType.Brush: this.Cursor = _cursorPencil; break;
                    case ToolType.Bucket: this.Cursor = _cursorBucket; break;
                    case ToolType.Selection: this.Cursor = _cursorCross; break;
                }
            }
        }

        /// <summary>
        /// Refresh mouse position
        /// </summary>
        public void RefreshPosition()
        {
            // If editing layers and using a tile tool
            if (_editMode == EditType.Layers && (_toolMode == ToolType.Brush || _toolMode == ToolType.Bucket))
            {
                // Get snapped position
                Point mouse = PointToClient(Cursor.Position);
                Size tileSize = _background.TileSize;
                Point snap = GetTranslatedSnappedPoint(mouse, tileSize);

                // Set tile id string
                SetMouseSector = GetTile(mouse.X, mouse.Y);

                // Check that the mouse is within room bounds
                if (CheckBounds(mouse.X, mouse.Y) == false)
                {
                    // Force redraw
                    Invalidate();
                    return;
                }

                // Set new position
                _posX = snap.X;
                _posY = snap.Y;
            }
        }

        #endregion

        #region Tile

        /// <summary>
        /// Gets the tile id at the desired position.
        /// </summary>
        /// <param name="x">Mouse X position.</param>
        /// <param name="y">Mouse Y position.</param>
        private string GetTile(int x, int y)
        {
            // If not within bounds of the layers array or room, return empty tile id.
            if ( _layerIndex < 0 || _layerIndex >= ProjectManager.Room.Layers.Count || CheckBounds(x, y) == false)
                return "Tile: -NA-";

            // Calculate tilesize.
            Size tileSize = _background.TileSize;

            // Get snapped position.
            Point snap = GetTranslatedSnappedPoint(new Point(x, y), tileSize);

            // Get column and row.
            int col = snap.X / _background.TileWidth;
            int row = snap.Y / _background.TileHeight;

            // Return tile id.
            return ProjectManager.Room.Layers[_layerIndex].Tiles[col, row].ToString();
        }

        /// <summary> 
        /// Get a selection of tiles from the selected layer.
        /// </summary>
        /// <param name="grid">The grid to use for selection data.</param>
        /// <returns>An array of tiles.</returns>
        private GMareTile[,] GetTiles(GMareBrush grid)
        {
            // A new array of tile ids.
            Rectangle rect = grid.ToRectangle();
            Size tileSize = _background.TileSize;
            GMareTile[,] tiles = new GMareTile[rect.Width / tileSize.Width, rect.Height / tileSize.Height];

            // Iterate through columns.
            for (int col = 0; col < tiles.GetLength(0); col++)
            {
                // Iterate through rows.
                for (int row = 0; row < tiles.GetLength(1); row++)
                {
                    // Calculate source position.
                    int x = ((col * tileSize.Width) + rect.X) / tileSize.Width;
                    int y = ((row * tileSize.Height) + rect.Y) / tileSize.Height;

                    // Set tile id.
                    tiles[col, row] = ProjectManager.Room.Layers[_layerIndex].Tiles[x, y].Clone();
                }
            }

            // Return selected tiles.
            return tiles;
        }

        /// <summary>
        /// Sets a tile index based on mouse coordinates.
        /// </summary>
        /// <param name="x">Mouse X position.</param>
        /// <param name="y">Mouse Y position.</param>
        private void SetTiles(int x, int y, bool setEmpty, GMareBrush tiles, bool absolute)
        {
            // If tile selection is empty, return
            if (tiles == null)
                return;

            // Calculate tilesize.
            Size tileSize = _background.TileSize;

            // Set snap point.
            Point snap = new Point(x, y);

            // If snap has not been pre-calculated, calculate snapped position.
            if (absolute == false)
                snap = GetTranslatedSnappedPoint(new Point(x, y), tileSize);

            // Iterate through columns.
            for (int col = 0; col < tiles.Tiles.GetLength(0); col++)
            {
                // Iterate through rows.
                for (int row = 0; row < tiles.Tiles.GetLength(1); row++)
                {
                    // Calculate destination tile position.
                    int destCol = (snap.X / tileSize.Width) + col;
                    int destRow = (snap.Y / tileSize.Height) + row;

                    // If index not within bounds, continue.
                    if (destRow < 0 || destCol > ProjectManager.Room.Columns - 1 ||
                        destCol < 0 || destRow > ProjectManager.Room.Rows - 1)
                        continue;

                    // If set empty is true, set tile id to -1, else set the target tile.
                    if (setEmpty == true)
                        ProjectManager.Room.Layers[_layerIndex].Tiles[destCol, destRow] = new GMareTile();
                    else
                        ProjectManager.Room.Layers[_layerIndex].Tiles[destCol, destRow] = tiles.Tiles[col, row].Clone();
                }
            }
        }

        #endregion

        #region Math

        /// <summary>
        /// Check if coordinates are within the room rectangle
        /// </summary>
        /// <param name="x">The horizontal coordinate</param>
        /// <param name="y">The vertical coordinate</param>
        /// <returns>False if out of bounds, else true</returns>
        private bool CheckBounds(int x, int y)
        {
            // Get actual position
            Point p1 = GetActualPoint(x, y);

            // If the coordinate is out of bounds
            if (p1.X < 0 || p1.X > ProjectManager.Room.Width - 1 || p1.Y < 0 || p1.Y > ProjectManager.Room.Height - 1)
            {
                _showCursor = false;
                return false;
            }

            // Show the cursor it is withing bounds
            _showCursor = true;

            // Within bounds
            return true;
        }

        /// <summary>
        /// Gets the actual point within the room
        /// </summary>
        /// <param name="x">The relative horizontal coordinate</param>
        /// <param name="y">The relative vertical coordinate</param>
        /// <returns>A point within the room</returns>
        private Point GetActualPoint(int x, int y)
        {
            // Create a new point
            Point point = new Point();

            // Calculate position with scroll offset
            int offsetX = (int)(Offset.X * Zoom);
            int offsetY = (int)(Offset.Y * Zoom);
            point.X = (int)((x + offsetX) / Zoom);
            point.Y = (int)((y + offsetY) / Zoom);

            // Return area corrected point
            return point;
        }

        /// <summary>
        /// Translates a point with scaling and scrolling values considered
        /// </summary>
        /// <param name="point">Point to translate</param>
        /// <returns>A Translated point</returns>
        public Point GetTranslatedPoint(Point point)
        {
            // Calculate snapped position
            int offsetX = (int)(Offset.X * Zoom);
            int offsetY = (int)(Offset.Y * Zoom);
            int x = (int)((point.X + offsetX) / Zoom);
            int y = (int)((point.Y + offsetY) / Zoom);

            return new Point(x, y);
        }

        /// <summary>
        /// Calculates a snapped version of a point
        /// </summary>
        /// <param name="position">Point to use as snapping origin</param>
        /// <param name="snap">Snapping value</param>
        /// <returns>A snapped point</returns>
        private Point GetTranslatedSnappedPoint(Point position, Size snap)
        {
            // Calculate snapped position
            int width = (int)(snap.Width * Zoom);
            int height = (int)(snap.Height * Zoom);
            int offsetX = (int)(Offset.X * Zoom);
            int offsetY = (int)(Offset.Y * Zoom);
            int x = (int)((((position.X + offsetX) / width) * width) / Zoom);
            int y = (int)((((position.Y + offsetY) / height) * height) / Zoom);

            return new Point(x, y);
        }

        /// <summary>
        /// Gets the smallest drawing area. Room size versus client size
        /// </summary>
        /// <returns>The smallest drawing size</returns>
        private Size GetSmallestCanvas()
        {
            // Set size to client size
            Size size = ClientSize;

            // if no project to compare to, return the client size
            if (ProjectManager.Room == null)
                return size;

            // Check for the smallest width
            if (ClientSize.Width > (int)(ProjectManager.Room.Width * Zoom))
                size.Width = (int)(ProjectManager.Room.Width * Zoom);

            // Check for the smallest height
            if (ClientSize.Height > (int)(ProjectManager.Room.Height * Zoom))
                size.Height = (int)(ProjectManager.Room.Height * Zoom);

            // Return the smallest possible drawing area
            return size;
        }

        /// <summary>
        /// Reacquires the tools origin position
        /// </summary>
        public void ReAcquirePosition()
        {
            // If a room has not been loaded, return
            if (CanDraw() == false || _background == null)
                return;

            // Get snapped position
            Point pos = GetTranslatedSnappedPoint(this.PointToClient(Cursor.Position), _background.TileSize);

            // Set cursor position
            _posX = pos.X;
            _posY = pos.Y;

            // Set the cursor
            SetCursor();

            // Force redraw
            Invalidate();
        }

        /// <summary>
        /// Gets a rectangle that bounds all the listed instances
        /// </summary>
        /// <param name="instances">A list of instances</param>
        /// <returns>A rectangle bound of the instances listed</returns>
        private Rectangle GetInstanceRectangle(List<GMareInstance> instances)
        {
            // Create a new rectangle, set base position to start with if an instance exists
            Rectangle rect = instances.Count > 0 ? new Rectangle(instances[0].X, instances[0].Y, 0, 0) : Rectangle.Empty;

            // Linq method
            //rect.X = instances.Min(i => i.X);
            //rect.Y = instances.Min(i => i.Y);
            //rect.Width = instances.Max(i => (i.X + (ProjectManager.Room.Objects.Find(o => o.Resource.Id == i.ObjectId) == null ? 0 : ProjectManager.Room.Objects.Find(o => o.Resource.Id == i.ObjectId).Image.Width))) - rect.X;
            //rect.Height = instances.Max(i => (i.Y + (ProjectManager.Room.Objects.Find(o => o.Resource.Id == i.ObjectId) == null ? 0 : ProjectManager.Room.Objects.Find(o => o.Resource.Id == i.ObjectId).Image.Height))) - rect.Y;

            // Iterate through instances
            foreach (GMareInstance instance in instances)
            {
                // Set minimum position of selected instances
                rect.X = Math.Min(rect.X, instance.X);
                rect.Y = Math.Min(rect.Y, instance.Y);
            }

            // Iterate through instances
            foreach (GMareInstance instance in instances)
            {
                // Get object associated with the instance, for image dimensions
                GMareObject obj = ProjectManager.Room.Objects.Find(o => o.Resource.Id == instance.ObjectId);

                // If no object was found or has not image data, return
                if (obj == null || obj.Image == null)
                    continue;

                // Set current maximum width and height of selected instances
                rect.Width = Math.Max(rect.Width, instance.X + obj.Image.Width);
                rect.Height = Math.Max(rect.Height, instance.Y + obj.Image.Height);
            }

            // Subtract rectangle origin offset
            rect.Width -= rect.X;
            rect.Height -= rect.Y;

            // Return bounds rectangle
            return rect;
        }

        #endregion

        #region Texture

        /// <summary>
        /// Loads a texture from a bitmap
        /// </summary>
        private void LoadTexture(Bitmap image)
        {
            // If no image exists, return
            if (image == null || ProjectManager.Room == null)
            {
                // Delete any previous tilemaps
                GraphicsManager.DeleteTilemaps();

                // If a brush exists, empty the tiles
                if (_brush != null)
                    _brush.Tiles = null;

                return;
            }

            // Delete any previous tilemaps
            GraphicsManager.DeleteTilemaps();

            // This is so that the bitmap is pre-rendered with "blending effects", instead of using OpenGL
            GraphicsManager.LoadTileMap(image, _background.TileWidth, _background.TileHeight);
            GraphicsManager.LoadTileMap(PixelMap.BitmapBrightness(image, -0.2f), _background.TileWidth, _background.TileHeight);
            GraphicsManager.LoadTileMap(PixelMap.BitmapTransparency(image, 0.5f), _background.TileWidth, _background.TileHeight);

            image.Dispose();
        }

        #endregion

        #region General

        /// <summary>
        /// Checks to see if requirements for rendering have been met
        /// </summary>
        /// <returns>If required data exists to render the control</returns>
        private bool CanDraw()
        {
            // If data exists to draw
            return ProjectManager.Room == null ? false : true;
        }

        /// <summary>
        /// Resets the control's selections, clipboards, and textures
        /// </summary>
        public void Reset()
        {
            _selection = null;
            _selectedObject = null;
            _selectionClip = null;
            _selectedInstances.Clear();
            _instanceClip.Clear();

            // Delete textures
            GraphicsManager.DeleteTextures();
            GraphicsManager.DeleteTilemaps();
        }

        /// <summary>
        /// Shows a warning message
        /// </summary>
        /// <param name="text">Text to display in the warning</param>
        private void ShowWarning(string text)
        {
            // Create a new warning message form
            using (WarningForm form = new WarningForm(text))
            {
                // Show dialog box
                form.ShowDialog();

                // If not showing the message, return
                if (!form.ShowMessage)
                    return;

                // Set message flag off
                if (text == GMare.Properties.Resources.BlendWarning)
                    ProjectManager.Room.BlendWarning = false;

                if (text == GMare.Properties.Resources.ScaleWarning)
                    ProjectManager.Room.ScaleWarning = false;
            }
        }

        #endregion

        #region Timer

        /// <summary>
        /// Selection marching ants timer tick
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            // Increase offset
            _stippleOffset++;

            // If at maximum offset, reset offset
            if (_stippleOffset % 8 == 0)
                _stippleOffset = 0;

            // As long as there is a selection, force redraw
            if (_selection != null)
                Invalidate();
        }

        #endregion

        #region Mouse Events

        #region Tiles

        /// <summary>
        /// Layers mode mouse down
        /// </summary>
        /// <param name="mouse">Mouse event arguments</param>
        private void LayersMouseDown(MouseEventArgs mouse)
        {
            // Get snapped position
            Size tileSize = _background.TileSize;
            Point snap = GetTranslatedSnappedPoint(mouse.Location, tileSize);

            // Check that the mouse is within room bounds
            if (CheckBounds(mouse.X, mouse.Y) == false || _layerIndex == -1)
                return;

            // Do action based on tool
            switch (_toolMode)
            {
                case ToolType.Brush:

                    // If left click
                    if (mouse.Button == MouseButtons.Left)
                    {
                        // Room is about to change, record it
                        RoomChanging();

                        // If the shift key is being held down erase tile, else paint tile
                        if (_shiftKey == true)
                            SetTiles(mouse.X, mouse.Y, true, _brush, false);
                        else
                            SetTiles(mouse.X, mouse.Y, false, _brush, false);
                    }
                    else if (mouse.Button == MouseButtons.Right)  // Show brush menu options
                        ShowBrushMenu(mouse.Location);

                    // Force redraw
                    Invalidate();
                    break;

                case ToolType.Bucket:

                    // If left click
                    if (mouse.Button == MouseButtons.Left)
                    {
                        // Room is changing
                        RoomChanging();

                        // Calculate starting point in tiles
                        Point tile = new Point(snap.X / tileSize.Width, snap.Y / tileSize.Height);

                        // If the shift key is being held down, erase tiles else, fill with brush
                        if (_shiftKey == true)
                            ProjectManager.Room.Layers[_layerIndex].Fill(tile, -1);
                        else
                            ProjectManager.Room.Layers[_layerIndex].Fill(tile, _brush.Tiles);

                        // Update the block instances
                        ProjectManager.Room.UpdateBlockInstances();
                    }
                    else if (mouse.Button == MouseButtons.Right)  // Show brush menu options
                        ShowBrushMenu(mouse.Location);

                    // Force redraw
                    Invalidate();
                    break;

                case ToolType.Selection:

                    // If left mouse button click
                    if (mouse.Button == MouseButtons.Left)
                    {
                        // If the selection is not empty, and the mouse is within the selection
                        if (_selection != null && _selection.ToRectangle().Contains(snap))
                        {
                            // Selection clicked
                            _moving = true;

                            // If the selection has never been clicked to be moved
                            if (_moved == false)
                            {
                                // Room is about to change, record it
                                RoomChanging();

                                // Set tiles empty under selection
                                SetTiles(_selection.ToRectangle().X, _selection.ToRectangle().Y, true, _selection, true);

                                // Set one time moving flag
                                _moved = true;
                            }

                            // Set zero position
                            _posX = snap.X;
                            _posY = snap.Y;
                        }

                        // If not moving an existing selection
                        if (_moving == false)
                        {
                            // If there is a previous selection, set it
                            if (_selection != null)
                                SetTiles(_selection.StartX, _selection.StartY, false, _selection, true);

                            // Create a new selection
                            _selection = new GMareBrush();

                            // Start collecting other tiles
                            _dragging = true;

                            // Set selection dimensions
                            _selection.StartX = snap.X;
                            _selection.StartY = snap.Y;
                            _selection.EndX = _selection.StartX + tileSize.Width;
                            _selection.EndY = _selection.StartY + tileSize.Height;

                            // Force redraw
                            Invalidate();
                        }
                    }
                    else if (mouse.Button == MouseButtons.Right)  // Show selection menu options
                    {
                        // Disable all options by default
                        for (int i = 0; i < mnuSelectionOptions.Items.Count; i++)
                            mnuSelectionOptions.Items[i].Enabled = false;

                        // If the selection is not empty
                        if (_selection != null)
                        {
                            // Allow options
                            mnuSelectionCopy.Enabled = true;
                            mnuSelectionCut.Enabled = true;
                            mnuSelectionDelete.Enabled = true;
                            mnuSelectionDeselect.Enabled = true;
                            mnuSelectionBrush.Enabled = true;
                            mnuSelectionAddBrush.Enabled = true;
                            mnuSelectionFlipHorizontal.Enabled = true;
                            mnuSelectionFlipVertical.Enabled = true;
                            mnuSelectionColor.Enabled = true;
                        }

                        // If the clipboard is empty, do not enable the paste function, else allow it
                        mnuSelectionPaste.Enabled = _selectionClip == null ? false : true;

                        // Show menu
                        mnuSelectionOptions.Show(PointToScreen(mouse.Location));
                    }

                    break;
            }
        }

        /// <summary>
        /// Layers mode mouse move
        /// </summary>
        private void LayersMouseMove(MouseEventArgs mouse)
        {
            // Get snapped position.
            Size tileSize = _background.TileSize;
            Point snap = GetTranslatedSnappedPoint(mouse.Location, tileSize);

            // Set tile id string.
            SetMouseSector = GetTile(mouse.X, mouse.Y);

            // Check that the mouse is within room bounds
            if (CheckBounds(mouse.X, mouse.Y) == false)
            {
                // Force redraw.
                Invalidate();
                return;
            }

            // Do action based on tool mode.
            switch (_toolMode)
            {
                case ToolType.Brush:

                    // If the new snapped position differs from the old position
                    if (snap.X != _posX || snap.Y != _posY)
                    {
                        // Set new position check.
                        _posX = snap.X;
                        _posY = snap.Y;

                        // If left click set tile
                        if (mouse.Button == MouseButtons.Left && _layerIndex != -1)
                        {
                            // If the shift key is being held down.
                            if (_shiftKey == true)
                                SetTiles(mouse.X, mouse.Y, true, _brush, false);
                            else
                                SetTiles(mouse.X, mouse.Y, false, _brush, false);
                        }

                        // Force redraw
                        Invalidate();
                    }

                    break;

                case ToolType.Selection:

                    // If a selection exists, cursor is within selection rectangle, and not dragging selection rectangle
                    if (_selection != null && _selection.ToRectangle().Contains(snap) == true && _dragging == false)
                        this.Cursor = Cursors.SizeAll;
                    else
                        this.Cursor = _cursorCross;

                    // If moving a selection
                    if (_moving)
                    {
                        // If there is a change in snapped position since last movement
                        if (snap.X != _posX || snap.Y != _posY)
                        {
                            // Calculate move amount
                            Point pos = new Point(snap.X - _posX, snap.Y - _posY);

                            // Set check to new value
                            _posX = snap.X;
                            _posY = snap.Y;

                            // Set selection position
                            _selection.StartX += pos.X;
                            _selection.StartY += pos.Y;
                            _selection.EndX += pos.X;
                            _selection.EndY += pos.Y;

                            // Force redraw
                            Invalidate();
                        }
                    }

                    // If not dragging a rubberband rectangle, return
                    if (!_dragging)
                        return;

                    // If the snapped x is greater than the start x, add an extra tile width to contain the mouse cursor
                    if (snap.X >= _selection.StartX)
                        snap.X += _background.TileWidth;

                    // If the snapped y is greater than the start y, add an extra tile height to contain the mouse cursor
                    if (snap.Y >= _selection.StartY)
                        snap.Y += _background.TileHeight;

                    // If there is a change in snapped position since last movement
                    if (snap.X != _selection.EndX || snap.Y != _selection.EndY)
                    {
                        // If the end x coordinate is not equal to the start x coordinate, set it
                        if (snap.X != _selection.StartX)
                            _selection.EndX = snap.X;

                        // If the end y coordinate is not equal to the start y coordinate, set it
                        if (snap.Y != _selection.StartY)
                            _selection.EndY = snap.Y;
                    }

                    // Force redraw
                    Invalidate();
                    break;

                default:

                    // If the mouse snap position is different, update
                    if (snap.X != _posX || snap.Y != _posY)
                    {
                        // Set new check position
                        _posX = snap.X;
                        _posY = snap.Y;

                        // Force redraw
                        Invalidate();
                    }

                    break;
            }
        }

        /// <summary>
        /// Layers mode mouse up
        /// </summary>
        private void LayersMouseUp()
        {
            // If the layer index is invalid, return
            if (_layerIndex == -1)
                return;

            // Do action based on tool type
            switch (_toolMode)
            {
                // Brush tool
                case ToolType.Brush:

                    // Update the block instances
                    ProjectManager.Room.UpdateBlockInstances();
                    break;

                // Selection tool
                case ToolType.Selection:

                    // If dragging, get selected tile ids
                    if (_dragging == true)
                        _selection.Tiles = GetTiles(_selection);

                    // Stop moving and dragging operations
                    _moving = false;
                    _dragging = false;

                    // Force redraw
                    Invalidate();
                    break;
            }
        }

        #endregion

        #region Objects

        /// <summary>
        /// Instances mouse down
        /// </summary>
        /// <param name="mouse">Mouse event arguments</param>
        private void InstancesMouseDown(MouseEventArgs mouse)
        {
            // Get snapped position based on grid
            Point snap = GetTranslatedSnappedPoint(mouse.Location, new Size(_gridX, _gridY));

            // If mouse left button clicked
            if (mouse.Button == MouseButtons.Left)
            {
                // If rubberband rectangle activated
                if (_shiftKey == true)
                    _instanceRectangle.Location = GetActualPoint(mouse.X, mouse.Y);
                else
                {
                    // Check for instance at mouse
                    GMareInstance instance = GetInstance(mouse.Location);

                    // If clicking on nothing and more than one instance has already been selected
                    if (instance == null && _selectedInstances.Count > 1)
                    {
                        // Empty selection
                        SetSelectedInstance(null, false);
                        return;
                    }
                    // If clicking on nothing or if instance is not already selected
                    else if (instance == null || _selectedInstances.Contains(instance) == false)
                    {
                        SetSelectedInstance(instance, _controlKey ? true : false);
                    }

                    // If the new instance does not need to be snapped to the grid, set to actual coordinates
                    if (_snap == false)
                        snap = new Point((int)((mouse.Location.X + Offset.X) * Zoom), (int)((mouse.Location.Y + Offset.Y) * Zoom));

                    // Start dragging operation
                    _dragging = true;

                    // Set position
                    _posX = snap.X;
                    _posY = snap.Y;

                    // Force redraw
                    Invalidate();
                }
            }
            else if (mouse.Button == MouseButtons.Right)  // If mouse right button clicked
            {
                // Get instance from mouse position
                GMareInstance instance = GetInstance(mouse.Location);

                // If an instance is under the mouse and is not an already selected instance
                // Set selected instance by right click
                if (instance != null && _selectedInstances.Contains(instance) == false)
                    SetSelectedInstance(instance, false);

                // Show menu
                mnuInstanceOptions.Show(PointToScreen(mouse.Location));
            }
        }

        /// <summary>
        /// Instances mouse move
        /// </summary>
        /// <param name="mouse">Mouse event arguments</param>
        private void InstancesMouseMove(MouseEventArgs mouse)
        {
            // If holding the shift and left mouse button
            if (mouse.Button == MouseButtons.Left && _shiftKey == true && _instanceRectangle != Rectangle.Empty)
            {
                // Actual mouse position
                Point actual = GetActualPoint(mouse.X, mouse.Y);

                // Calculated the selection rubberband rectangle's position, force redraw
                _instanceRectangle.Size = new Size(actual.X - _instanceRectangle.X, actual.Y - _instanceRectangle.Y);
                Invalidate();
            }

            // If not doing a dragging operation, check for instances to report properties and return
            if (_dragging == false)
            {
                // Check for instance collision
                GetInstance(mouse.Location);
                return;
            }

            // Get snapped position, based on grid
            Point pos = GetTranslatedSnappedPoint(mouse.Location, new Size(_gridX, _gridY));

            // If the instance does not need to be snapped to the grid, set to actual coordinates
            if (_snap == false)
                pos = GetActualPoint(mouse.X, mouse.Y);

            // If there is a change in position since last movement
            if (_posX != pos.X || _posY != pos.Y)
            {
                // If instances have been selected
                if (_selectedInstances.Count > 0)
                {
                    // Iterate through instances
                    foreach (GMareInstance instance in _selectedInstances)
                    {
                        // If a block instance, it can't move, skip
                        if (instance.TileId != -1)
                            continue;

                        // Change the selected instance's position
                        instance.X = instance.X + ((pos.X - _posX));
                        instance.Y = instance.Y + ((pos.Y - _posY));
                    }
                }

                // Set new drag display position
                _posX = pos.X;
                _posY = pos.Y;

                // Force redraw
                Invalidate();
            }
        }

        /// <summary>
        /// Instances mouse up
        /// </summary>
        private void InstancesMouseUp()
        {
            // If a selection rectangle has been made
            if (_instanceRectangle != Rectangle.Empty)
            {
                // Convert the selection rectangle to positive values
                Rectangle rect = new Rectangle();
                rect.X = Math.Min(_instanceRectangle.X, _instanceRectangle.X + _instanceRectangle.Width);
                rect.Y = Math.Min(_instanceRectangle.Y, _instanceRectangle.Y + _instanceRectangle.Height);
                rect.Width = Math.Abs(_instanceRectangle.Width);
                rect.Height = Math.Abs(_instanceRectangle.Height);

                // Set all instances that intersect with the selection rectangle
                SetSelectedInstances(GetInstances(rect));

                // Empty the instance selection rectangle
                _instanceRectangle = Rectangle.Empty;

                // Force redraw
                Invalidate();
                return;
            }

            // If no object was selected, or not in a dragging operation, or there are instances selected return
            if (_selectedObject == null || _dragging == false || _selectedInstances.Count > 0)
                return;

            // Room changed, instance being added
            RoomChanging();

            // Create a new instance, based off of selected object
            GMareInstance inst = new GMareInstance(-1);
            inst.ObjectId = _selectedObject.Resource.Id;
            inst.ObjectName = _selectedObject.Resource.Name;
            inst.X = _posX - _selectedObject.OriginX;
            inst.Y = _posY - _selectedObject.OriginY;

            // Add the new instance
            ProjectManager.Room.Instances.Add(inst);

            // Set selected instance
            _selectedInstances.Clear();
            _selectedInstances.Add(inst);

            // Fire selected instance changed event
            SelectedInstanceChanged();

            // Force redraw
            Invalidate();
        }

        /// <summary>
        /// Gets a list of instances intersecting the source rectangle
        /// </summary>
        /// <param name="rect">The rectangle argument</param>
        /// <returns>A list of selected rectangles</returns>
        private List<GMareInstance> GetInstances(Rectangle rect)
        {
            // Selected instances
            List<GMareInstance> instances = new List<GMareInstance>();

            // Iterate through instances
            foreach (GMareInstance instance in ProjectManager.Room.Instances)
            {
                // If not showing block instances, and the instance is a block instance, continue
                if (!_showBlocks && instance.TileId != -1)
                    continue;

                // Get the instance rectangle
                Point pos = new Point((int)((instance.X * Zoom) / Zoom), (int)((instance.Y * Zoom) / Zoom));
                Size size = GraphicsManager.Sprites[instance.ObjectId].Size;
                size.Width *= (int)Zoom;
                size.Height *= (int)Zoom;

                // If the rectangle contains the sprite, Set dragging instance
                if (rect.IntersectsWith(new Rectangle(pos, size))) 
                    instances.Add(instance);
            }

            // Return the list of selected instances
            return instances;
        }

        /// <summary>
        /// Gets a single instance that contains the source point
        /// </summary>
        /// <param name="point">The point to check</param>
        private GMareInstance GetInstance(Point point)
        {
            // Offset scroll position
            point.X += (int)(Offset.X * Zoom);
            point.Y += (int)(Offset.Y * Zoom);

            // Iterate through instances backwards (Top items have more priority)
            for (int i = ProjectManager.Room.Instances.Count - 1; i > -1; i--)
            {
                // Get instance
                GMareInstance instance = ProjectManager.Room.Instances[i];

                // If not showing block instances, and the instance is a block instance, continue
                if (!_showBlocks && instance.TileId != -1)
                    continue;

                // Get the instance rectangle
                Size size = GraphicsManager.Sprites[instance.ObjectId].Size;
                Rectangle rect = new Rectangle((int)(instance.X * Zoom), (int)(instance.Y * Zoom), (int)(size.Width * Zoom), (int)(size.Height * Zoom));

                // If the rectangle contains the point
                if (rect.Contains(point))
                {
                    // Get location on the sprite
                    int x = point.X - (int)(instance.X * Zoom);
                    int y = point.Y - (int)(instance.Y * Zoom);

                    // Get the pixel from the sprite.
                    Color color = Color.FromArgb(GraphicsManager.Sprites[instance.ObjectId].Pixels[(int)(x / Zoom), (int)(y / Zoom)]);

                    // If not on a transparent pixel (Pixels arranged in GDI format) :P
                    if (color.B != 0)
                    {
                        // Set cursor
                        this.Cursor = Cursors.SizeAll;

                        // Set instance information
                        SetMouseInstance = instance.CreationCode.Length > 0 ? instance.Name == "" ? instance.ObjectName : instance.Name + ": " + "Has Code" :
                                                                              instance.Name == "" ? instance.ObjectName : instance.Name + ": " + "No Code";

                        // Return the instance that contains the point
                        return instance;
                    }
                }
            }

            // Set cursor
            this.Cursor = Cursors.Arrow;

            // Set instance information
            SetMouseInstance = "-NA-";

            // No instance contains the point
            return null;
        }

        /// <summary>
        /// Sets instances
        /// </summary>
        /// <param name="instances">The instances to set.</param>
        private void SetSelectedInstances(List<GMareInstance> instances)
        {
            // If the instance list is null, clear instances, return
            if (instances == null)
            {
                // Clear old instances
                _selectedInstances.Clear();
                return;
            }

            // Clear old instances
            _selectedInstances.Clear();

            // Set dragging instance
            _selectedInstances = instances;

            // Fire selected instance changed event
            SelectedInstanceChanged();
        }

        /// <summary>
        /// Sets instance
        /// </summary>
        /// <param name="instance">The instance to set</param>
        private void SetSelectedInstance(GMareInstance instance, bool addOverride)
        {
            // If a list of selected instances exist and not being overridde, clear old instances 
            if (_selectedInstances != null && !addOverride)
                _selectedInstances.Clear();

            // Set dragging instance
            if (instance != null)
                _selectedInstances.Add(instance);

            // Fire selected instance changed event
            SelectedInstanceChanged();

            // Force redraw
            Invalidate();
        }

        #endregion

        #endregion

        #region Brush Menu

        /// <summary>
        /// Shows the brush menu
        /// </summary>
        /// <param name="location">Where to draw the menu</param>
        private void ShowBrushMenu(Point location)
        {
            // Get a count of menu items
            int count = mnuBrushOptions.Items.Count - 1;

            // Remove all previous brushes at the given menu item index
            for (int i = count; i > 5; i--)
                mnuBrushOptions.Items.RemoveAt(i);

            // Set visible elements, based on brush count
            mnuBrushEdit.Visible = ProjectManager.Room.Brushes.Count == 0 ? false : true;
            mnuBrushNone.Visible = ProjectManager.Room.Brushes.Count == 0 ? true : false;

            // Iterate through brushes
            foreach (GMareBrush brush in ProjectManager.Room.Brushes)
            {
                // Create a menu item
                ToolStripMenuItem item = new ToolStripMenuItem(brush.Name, brush.Glyph);

                // Set tag with brush data
                item.Tag = brush;

                // Add item
                mnuBrushOptions.Items.Add(item);
            }

            // Show brush menu
            mnuBrushOptions.Show(PointToScreen(location));
        }

        #endregion

        #endregion
    }
}