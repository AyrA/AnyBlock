# AnyBlock

Blocks IP ranges in Windows firewall based on owner information.

## IP Ranges

The IP ranges are obtained from https://cable.ayra.ch/ip/global.php

The application will attempt to update the list on each run if it's missing or older than 24 hours.

## Usage

AnyBlock supports command line and GUI usage.

## GUI

The GUI is automatically launched if you run AnyBlock without any arguments.

Right now you can add and remove ranges to the block list.

## Command Line Mode

Command line format overview:

    AnyBlock.exe [/v] [/clear | /config | /add range [...] |
		/remove name [...] | /apply [v{4|6}] | /list] |
		/export {csv|tsv|p2p|json}

### `/v`

Enables more verbose logging to console.
If supplied, the console will also be shown in GUI mode.
This parameter is implied if you compile a debug build.

### `/config`

Lists currently configured ranges and directions on the console.

### `/add`

Adds the specified range(s) to the list.
A range is formatted as `dir:name`

- **dir**: One of IN,OUT,BOTH,DISABLED
- **name**: fully qualified node name

Disabled entries behave as if they were not configured at all,
meaning that `/apply` will still remove them from existing rules.

It's not necessary to disable an entry to delete it.
Disabling is merely a way to temporarily remove a range.

To change the direction of an existing entry,
add it again using a different direction.

As of now, this command will not check for unnecessary child rules when adding a parent rule.

### `/remove`

Removes the specified range(s) from the List.
Removing is done by name only. Directions can't be specified.

### `/apply`

Applies the configured list to firewall rules.
Applying the list will remove all blocked IPs that are no longer in the current list of addresses.
To keep the list accurate, this command should be run every 24 hours.

This command takes an optional argument that can either be `v4` or `v6`.
This causes the application to only add rules with the given IP address type.
This is especially useful if your server is only reachable via one of the two types.

### `/clear`

Removes all AnyBlock rules regardless of configuration.

### `/list`

Lists all available ranges

### `/export {csv|tsv|p2p|json}`

Exports the currently selected rules with IP ranges in various formats:

- **csv**: Exports name, start-IP, end-IP, cidr in csv format (with headers)
- **tsv**: Same as csv, but uses tab to delimit fields
- **p2p**: Peer to peer blocklist format for common P2P applications
- **json**: JSON object. Range names are keys and the CIDR list the values

