#!/usr/bin/env swift
// Renders the Bee.DefineEditor macOS app icon:
//   * Plain white squircle background
//   * Seven-cell honeycomb (1 centre + 6 surrounding hexagons) in amber yellow
//   * Pointy-top hexagons with thin white gaps for the honeycomb grid feel
//
// Output: ten PNGs at the sizes Apple's iconset spec requires (16/32/128/256/512
// at @1x and @2x). Pipe through `iconutil -c icns AppIcon.iconset` afterwards
// to produce AppIcon.icns.
//
// Usage:
//   swift build-icon.swift <output_iconset_dir>
//   iconutil -c icns <output_iconset_dir>

import AppKit
import Foundation

guard CommandLine.arguments.count == 2 else {
    FileHandle.standardError.write("usage: build-icon.swift <output_iconset_dir>\n".data(using: .utf8)!)
    exit(2)
}

let outputDir = CommandLine.arguments[1]
try? FileManager.default.createDirectory(atPath: outputDir, withIntermediateDirectories: true)

let sizes: [(name: String, px: CGFloat)] = [
    ("icon_16x16.png",       16),
    ("icon_16x16@2x.png",    32),
    ("icon_32x32.png",       32),
    ("icon_32x32@2x.png",    64),
    ("icon_128x128.png",    128),
    ("icon_128x128@2x.png", 256),
    ("icon_256x256.png",    256),
    ("icon_256x256@2x.png", 512),
    ("icon_512x512.png",    512),
    ("icon_512x512@2x.png", 1024),
]

// Deep slate blue background — pairs with amber by being its near-complement
// on the colour wheel, so the honeycomb pops without needing extra effects.
let backgroundColor = NSColor(red: 0.17, green: 0.24, blue: 0.31, alpha: 1.0)  // #2C3E50
// Amber that reads clearly against the slate background at 16 px while still
// feeling warm at 1024 px.
let combColor       = NSColor(red: 1.00, green: 0.76, blue: 0.10, alpha: 1.0)  // #FFC219

// Builds a pointy-top regular hexagon as an NSBezierPath. `radius` is the
// distance from centre to vertex (i.e. the circumradius). The first vertex
// sits straight up at +Y, so the cell has flat sides on left and right.
func hexagon(centerX: CGFloat, centerY: CGFloat, radius: CGFloat) -> NSBezierPath {
    let path = NSBezierPath()
    for i in 0..<6 {
        // Start at the top (pi/2), step 60° per vertex.
        let angle = .pi / 2 + .pi / 3 * CGFloat(i)
        let x = centerX + radius * cos(angle)
        let y = centerY + radius * sin(angle)
        if i == 0 {
            path.move(to: NSPoint(x: x, y: y))
        } else {
            path.line(to: NSPoint(x: x, y: y))
        }
    }
    path.close()
    return path
}

func renderIcon(px: CGFloat) -> NSBitmapImageRep {
    let rep = NSBitmapImageRep(
        bitmapDataPlanes: nil,
        pixelsWide: Int(px),
        pixelsHigh: Int(px),
        bitsPerSample: 8,
        samplesPerPixel: 4,
        hasAlpha: true,
        isPlanar: false,
        colorSpaceName: .deviceRGB,
        bytesPerRow: 0,
        bitsPerPixel: 32
    )!

    NSGraphicsContext.saveGraphicsState()
    NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: rep)

    // Background squircle.
    let radius = px * 0.225
    let rect = NSRect(x: 0, y: 0, width: px, height: px)
    NSBezierPath(roundedRect: rect, xRadius: radius, yRadius: radius).addClip()
    backgroundColor.setFill()
    rect.fill()

    // Layout in 1024-unit design space, scaled to px.
    let s = px / 1024.0
    let cx = px / 2
    let cy = px / 2

    // Hexagon geometry. `cellR` is the circumradius for layout (centre-to-
    // centre spacing is computed from it); the visible cell is drawn slightly
    // smaller so neighbouring cells leave a thin white gap — that gap is what
    // makes the seven shapes read as a honeycomb grid rather than one blob.
    let cellR: CGFloat = 175 * s          // layout radius
    let visibleR: CGFloat = cellR * 0.93  // shrink slightly for the gap
    let sqrt3 = CGFloat(sqrt(3.0))

    // Six surrounding cells around the centre. Order doesn't matter.
    let positions: [(dx: CGFloat, dy: CGFloat)] = [
        ( 0,                  0),                   // centre
        ( sqrt3 * cellR,      0),                   // right
        ( sqrt3 * cellR / 2,  1.5 * cellR),         // upper-right
        (-sqrt3 * cellR / 2,  1.5 * cellR),         // upper-left
        (-sqrt3 * cellR,      0),                   // left
        (-sqrt3 * cellR / 2, -1.5 * cellR),         // lower-left
        ( sqrt3 * cellR / 2, -1.5 * cellR),         // lower-right
    ]

    combColor.setFill()
    for pos in positions {
        let path = hexagon(centerX: cx + pos.dx, centerY: cy + pos.dy, radius: visibleR)
        path.fill()
    }

    NSGraphicsContext.restoreGraphicsState()
    return rep
}

for (name, px) in sizes {
    let rep = renderIcon(px: px)
    guard let pngData = rep.representation(using: .png, properties: [:]) else {
        FileHandle.standardError.write("error: failed to encode \(name)\n".data(using: .utf8)!)
        exit(1)
    }
    let url = URL(fileURLWithPath: outputDir).appendingPathComponent(name)
    try pngData.write(to: url)
    print("  wrote \(name) (\(Int(px))px)")
}

print("Done. Run: iconutil -c icns \(outputDir)")
