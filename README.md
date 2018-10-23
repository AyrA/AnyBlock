# AnyBlock

Blocks the given IP Ranges in Windows Firewall

## IP Ranges

The IP Ranges are obtained from https://cable.ayra.ch/ip/global.php

The Application will attempt to update the list on each run if it's missing or older than 24 Hours.

## Usage

AnyBlock supports Command Line and GUI usage.

## GUI

The GUI is automatically launched if you run AnyBlock without any Arguments.

Right now you can add and remove Ranges to the Block List

## Command Line Mode

Command Line:

    AnyBlock.exe [/v] [/clear | /config | /add range [...] | /remove name [...] | /apply | /list]

### `/v`

Enables more verbose Logging to Console.
If supplied, the Console will also be shown in GUI Mode

### `/config`

Lists currently configured Ranges and directions on the console

### `/add`

Adds the specified Range(s) to the List.
A range is formatted as `dir:name`

- **dir**: One of IN,OUT,BOTH,DISABLED
- **name**: fully qualified Node Name

Disabled entries behave as if they were not configured at all,
meaning that `/apply` will remove them from existing Rules.

It's not necessary to disable an entry to delete it.
Disabling is merely a way to temporarily removing a range.

To change the direction of an existing entry,
add it again using a different direction.

As of now, this command will not check for unnecessary child rules when adding a parent rule.

### `/remove`

Removes the specified Range(s) from the List.
Removing is done by name only. Directions can't be specified.


### `/apply`

Applies List to Firewall Rules.
Applying the List will remove all blocked IPs that are no longer in the current List of Addresses.
To keep the List accurate, this Command should be run every 24 hours.

### `/clear`

Removes all AnyBlock Rules regardless of Configuration

### `/list`

Lists all available Ranges
