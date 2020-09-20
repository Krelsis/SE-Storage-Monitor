

# SE-Storage-Monitor
A small in-game script for use in the Programmable Block in the game : [Space Engineers](https://store.steampowered.com/app/244850/Space_Engineers/), whose purpose is to monitor storage and provide a small subset of actions based on storage capacity.

## Install via Steam Workshop
To install this script, please visit the workshop page for the script [here](https://steamcommunity.com/sharedfiles/filedetails/?id=1940618097)  and subscribe to the workshop item. The script should then appear when browsing for scripts on the programmable block.

## Introduction
Ever wanted to know the state of your storage capacity of your base/ship? Well good news, you can do manually clicking between each and every container checking it's Volume/Max volume to figure out how much more it can carry! Boring right?  
  
Ever wanted to know your cargo capacity at a glance without having to manually check the containers?  
  
This script does it for you!  
  
  ## Features 

 1. Collects data on *all* containers with an inventory.  
 2. Calculates Current Mass, Current Volume, Max Volume of the entire (local) grid.  
 3. Allows you to output all or one of the above data on an LCD or cockpit screen. (By default shows all of the information on the Program block screen)  
 4. Allows you to specify behavioural changes of blocks that can be toggled depending on current storage capacity (fixed values right now) requires the use of grouping (see below)  
 5. Every container will have a percentage appended at the end of it's name so that you can see the capacity of individual containers without opening them  
  
## How to use   
  
Create a Program block. Install this script. `[StorageMonitor]` will be appended into the Programmable Blocks name, this is an indicator that the script is working, the summary of your grids storage will be shown on the program block and it's screen.  
  
------------------------------------------------------------------------  
#### LCDs :  
------------------------------------------------------------------------  
You may choose **ONE** of the tags below to add to an LCD  
Either put the tag in the Custom Data section or straight into the name (end of name recommended, Custom Data preferred) of the LCD.  
  
**[StorageMass]** : The mass of all items currently stored (useful for determining the weight/mass of stored items, rather than the weight of the entire grid (incl. Inventories and mass of Blocks themselves)
**[StorageVolume]** : The current volume of all stored items.  
**[StorageMaxVolume]** : The maximum volume of items that can be stored  
**[StorageSummary]** : Shows all gathered data (all of above) on screen. (Also provides a percentile run down on capacity i.e. 80% used)  
**[StorageStatus]** : Same as [StorageSummary]  
  
------------------------------------------------------------------------  
#### Cockpit Screens:  
------------------------------------------------------------------------    
Same as above with LCD's however after the tag append a colon ( : ) followed by a number, this number represent the screen that this data should be shown on. This number starts from 0. I.e. [StorageSummary]:0 would show the storage summary on the first screen in the cockpit while [StorageMass]:3 would show the mass on the fourth screen.  
  
------------------------------------------------------------------------  
#### Ignoring Containers/Devices:  
------------------------------------------------------------------------  

Add the tag **[StorageIgnore]** to either the Name or Custom Data and the script will not touch this device.   

  
------------------------------------------------------------------------    
#### Selective Grouping:  
------------------------------------------------------------------------  
  
*So you want to track **SPECIFIC** containers instead of all containers?*  

No problem, just add the tag `[StorageResponsibility]` to the container **AND** program block to show that you are specifying a new Responsibility/Group.  
  
Follow this tag up with a colon ( **:** ) and a tag of your choosing. I.E. `[StorageResponsibility]:[Test]` would tag the container and script as `[Test]` and the script will only track the container with the Test tag.  
  
This can benefit you if you want to set up different scripts for different containers i.e. seeing states on ores/ingot containers compared to components containers, rather than all containers.  
  
  
------------------------------------------------------------------------  
#### Turning on/off miscellaneous blocks in a group:  
  ------------------------------------------------------------------------  
  
Scenario: You have some lighting set up around the base, you have several lights which look like hazard lights but are red near your cargo area which you want to turn on when cargo is full indicating the need to clear up some space or add more containers.  
  
1. First set up your light as you wish.  
2. Add `[StorageMonitor]` as a tag.  
3. Add the device to the selective grouping group. I.e. `[StorageResponsibility]:[Test]`   
4. Choose and add **ONE** of the following tags :  
  
**[OffWhenTotalStorageFull]** : Device turns OFF when total capacity is above 98%. Otherwise, the device is ON.  

**[OnWhenTotalStorageFull]** : Same as above, but inverted. 
  
**[OffWhenTotalStorageMoreThan]:###** : Device turns OFF when total capacity is above specified value as a percentage % (to specify a value, follow the tag with a colon ( : ) and a number. Otherwise the device is ON.  

**[OnWhenTotalStorageMoreThan]:###** : Same as above, but inverted.
  
**[OffWhenTotalStorageMoreThanEqualTo]:###** : Device turns OFF when total capacity is at (equal to) or above the specified value as a percentage % (to specify a value, follow the tag with a colon ( : ) and a number. Otherwise the device is ON.  
  
**[OnWhenTotalStorageMoreThanEqualTo]:###** : Same as above, but inverted.
  
------------------------------------------------------------------------  
#### Default grouping:  
------------------------------------------------------------------------  
  
If you have not specified a group then the group is set to **[Default]** to interact with this group/responsibilty you can do so with `[StorageResponsibility]:[Default]`.  This saves time and hassle if your intent is to use this script to track ALL cargo containers, you can just use the Default group/responsibility.
  
Using the above exmaple regarding the lights, the lights will have to include `[StorageResponsibility]:[Default]` as they are a miscellaneous block and won't be controlled automatically. However, the programmable block, cockpits, lcd's and cargo containers will be controlled/assigned to the Default group automatically and will not have to be assigned to the group.