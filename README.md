# Foor Controller MK I

## Introduction

This device allows you to type keyboard or mouse button inputs with your foot (tilt angle).

## Getting Started

1. Download and run [FC Options.exe](https://github.com/tylim2946/Foot-Controller-Mk-I/blob/main/FC-Options/Debug/FC%20Options.exe)
2. Click `Device...` and select your device from the list
3. Click `Add Profile...` to create a new profile
4. Click `Assign Key` to map keys
5. Manually enter angular position values or use `Detect` to automatically set the values from two points
6. Click `Set Reference` to initialize your current position

## Device

<table>
	<tr>
		<td><img src="https://github.com/tylim2946/Foot-Controller-Mk-I/blob/main/images/completed_prototype (1).jpg" width="480" height="270"></td>
		<td><img src="https://github.com/tylim2946/Foot-Controller-Mk-I/blob/main/images/completed_prototype (2).jpg" width="480" height="270"></td>
	</tr>
</table>

The case was designed in Fusion 360 and was 3D printed using Tinkerine Ditto Pro 3D printer.

## FC Options (Windows Application)

<table>
	<tr>
		<td><img src="https://github.com/tylim2946/Foot-Controller-Mk-I/blob/main/images/Screenshot%20(1).png" width="480" height="270"></td>
		<td><img src="https://github.com/tylim2946/Foot-Controller-Mk-I/blob/main/images/Screenshot%20(2).png" width="480" height="270"></td>
	</tr>
</table>

The software was developed using Visual Studio and WinForms. The software will receive the 3-axes tilt angle information from the Foot Controller device (through Bluetooth using 32feet.NET library), and convert them into keystrokes using InputManager library.

## Libraries Used

- [32feet.NET](https://github.com/inthehand/32feet)
- [InputManager](https://www.codeproject.com/Articles/117657/InputManager-library-Track-user-input-and-simulate)
