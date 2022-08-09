# vatSysApi

#### Api Plugin Install

The API plugin to be placed in vatSys plugins directory.  

For example: "[...]\Documents\vatSys Files\Profiles\Australia" or "[...]\Program Files (x86)\vatSys\bin\Plugins"

#### Discord Extension Install

The Discord App to be placed in: "[...]\Documents\vatSys Files\Discord".

The Api plugin must also be installed for the Discord extensions to work.

#### Uri

The API plugin runs at http://localhost:45341.

#### GET: /Version

Returns the version of the Api plugin.

#### GET: /Details

Returns the details of the connected user including frequencies, transmissions and aircraft spotted.

#### GET: /Aircraft

Returns the details of all current aircraft.

#### GET: /Aircraft/{callsign}

Returns the details of the aircraft with the {callsign} specified.

#### POST: /Aircraft/{callsign}/Squawk

Sets the squawk code for the aircraft with the {callsign} specified to a randomly allocated code.

#### POST: /Aircraft/{callsign}/Squawk/{code}

Sets the squawk code for the aircraft with the {callsign} specified to the {code}.

#### POST: /Aircraft/{callsign}/CFL/{level}

Sets the cleared flight level for the aircraft with the {callsign} specified to the {level}.
