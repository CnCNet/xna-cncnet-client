#!/bin/sh
wineconsole Resources/Compatibility/Unix/wine-game.bat &
BACK_PID=$!
wait $BACK_PID
