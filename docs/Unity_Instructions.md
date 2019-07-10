# Adding Game Telemetry to your project
## Setup
1.	Create a directory called **GameTelemetryPlugins** in your project's **\Assets\Scripts** directory and copy the **Game Telemetry** folder into that directory.

-- or --

1.  Open the GameTelemetry Unity package

2.	Open **TelemetrySettings.cs** and set your injest URL, query URL, and auth key

Your project is now ready for game telemetry.

 ---
## Adding events
1.	You can access the recording interface by using the **GameTelemetry** namespace

2.  To start, initialize the system with a game object that will be consistantly in the game, such as a player. 

```cs
using GameTelemetry;

Telemetry.Initialize(myGameObject);
```

2.	While any event attribute can be overwritten, the following are populated automatically in the plugin based on information available to the engine:

    + Build Type
    + Platform
    + Client ID
    + Client Timestamp
    + Session ID
    + Build ID
    + Process ID
    + Sequence ID

3.	Use TelemetryBuilder to assign properties to your event

Example:
```cs
TelemetryBuilder Builder = new TelemetryBuilder();
Builder.SetProperty(Telemetry.Position(settings.position));
Builder.SetProperty(Telemetry.Orientation(settings.rotation.eulerAngles));
```

4.	Record the event, providing the required name, category, event version, and your properties

Example:
```cs
Telemetry.Record(”Health”, ”Gameplay”, ”1.3”, Builder);
```
With that, your event will be sent with the next batch send (set by SendInterval during setup)

---
## Making your events visualizer friendly
While you can record any event you want, the ability to view it using the GameTelemetry plugin requires a couple of settings:

1.	The position of the event is required in order to know where the event can be drawn.  Orientation is also supported

Example:
```cs
Telemetry.Position(settings.position);
Telemetry.Orientation(settings.rotation.eulerAngles);
```

2.	If your event has related data that can be used by the heatmap generator (or that you would like attached to each event drawn), a “disp_val” property is needed to direct what event value is needed:

Example:
```cs
Telemetry.DisplayValue(”val_health”);
```

3.	The value that “disp_val” uses will also need to be populated

    + For percentages, either prefix your property with “pct_” or use the construction shortcut Percentage.  This will let the visualizer know that the value is a float between 0 and 100.
   
    + For all other values, prefix your property with “val_” or use the construction shortcut Value.

Example: 
```cs
Telemetry.Percentage(”health”, MyHealth)
```

Example:
```cs
Telemetry.Value(”health”, MyHealth)
```

A final, visualizer friendly event might look something like this:
```cs
TelemetryBuilder Builder = new TelemetryBuilder();

Builder.SetProperty(Telemetry.Position(settings.position);
Builder.SetProperty(Telemetry.Orientation(settings.rotation.eulerAngles);
Builder.SetProperty(Telemetry.Value("health", MyHealth));
Builder.SetProperty(Telemetry.DisplayValue(”val_health”));

Telemetry.Record(”Health”, ”Gameplay”, ”1.3”, Builder);
```

---
## Using the visualizer
Once you have data uploaded, you are ready to start visualizing it!

1. In the Unity editor, open your project.
2. Open the **Window** menu and select **Game Telemetry**

    ![Window](images/unity/window_menu.png)

3. A new window will open.  Note that these can be docked anywhere in the editor or combined within windows.

### Game Telemetry Window

4. First, we will use the **Event Settings** sections to get our first dataset.  Build a query for what general events you would like to recieve.  Once you are ready, press **Submit** and wait for the results.  If the query found events, you will see them populate in the **Event Search** section.
    
    ![Game Telemetry window](images/unity/data_viewer.png)

5. By default, all data received by the query will be enabled.  In the **Event Search** section, you can uncheck any event groups you do not wish to see and use the search bar above to look for different event names.  In addition, each event group has a changeable color and shape for how each event is drawn.
6. You should now have events being drawn directly in your game world.  Use the different shapes and colors to customize your view to see the most relivant information.  Also note that using shapes such as the capsule will provide orientation detail as well.
    
    ![Telemetry points](images/unity/points.png)

7. All of the events are actually interactive elements in the game.  They can be clicked to see further details, zoomed, saved, and more.  In the **Hierarchy** window, just look for game objects under the **GameTelemetry** section.
    
    ![Hierarchy](images/unity/world_outliner.png)

8. Now we will use the **Visualization Tools** section to get unique views of our data. Expand the drop down to see a list of event types, the same from the *Event Search* section above. Select one of those event groups.

9. You will notice that underneith, a time will now be at the end of our animation bar.  This is the elapsed time of the selected data selected.  Simply press any of the play/rewind buttons or drag the bar to any location in the timeline to see how your data has changed over time.  Points will appear or disappear as if they were happening in realtime.
      
       Note: If there is a gap in time greater than 30 seconds, the animation will skip ahead to keep the visualization more fluid
10. Now lets look at the *Heatmap Settings* area.  Here, you can select a variety of options to combine your event data in to a 3D map and even watch that collection of data animate over time.

    ![Telemetry heatmap](images/unity/heatmap.png)

11. Type represents the type of heatmap you would like to generate.
    + *Population* combines events into physical groups and displays a heatmap of the number of each event within a given group.
    + *Population - Bar* does the same, but generates a 3D bar graph over the XY plane.
    + *Value* also combines events into physical groups, but displays a heatmap of the average of the value of each event within a given group.
    + *Value - Bar* provides a 3D bar graph of value information.
12. Shape and shape size represent the shape of the heatmap elements along with their radius.  The smaller the radius, the more detailed the information (though also the more complex the heatmap is to generate).
13. The color range provides the color for the minimum and maximum values in the map.  Colors in between are a fade between the two colors chosen.
14. Type range will populate when you generate the heatmap, but can be used to adjust the colors.  This can be helpful when your data has outlying values that can cause the heatmap to lack variability.  Adjust the range to remove extreme highs and lows to get more interesting information.
15. Use Orientation will rotate the heatmap shapes to the average orientation of all events within each element.
16. Apply to Animation will allow the animation controls above control animating the entire heatmap

---
## Troubleshooting / FAQ

+ I am getting an error that System.Tuple is undefined
  + This issue can occur if you are attempting to build using a version of the .NET framework prior to 4.0.  Under File->Build Settings->PlayerSettings->OtherSettings->Configuration, change the target framework to use 4.0 or later.
