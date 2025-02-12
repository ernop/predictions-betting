#!/usr/bin/env python3

import graphviz
import random
from PIL import Image, ImageDraw, ImageFont
import os
import math

# Multiline string of the data exactly as given:
TABLE_TEXT = """
        Daffy   Ernie   Ivan    Jason
Daffy   -3      -3      -4      4    
Ernie   3       -1      0       6    
Ivan    0       -2      2       -5   
Jason   1       6       3       -5   
"""

def parse_table(table_str):
    """
    Parse the multiline table into a dictionary keyed by row-person, then column-person.
    We only extract the 4 "persons" (Daffy, Ernie, Ivan, Jason) as rows and columns.

    Returns:
        data[row_person][col_person] = integer_value
    """
    # The "valid" rows & columns we care about
    valid_people = ["Daffy", "Ernie", "Ivan", "Jason"]

    # Split lines (ignore blank lines)
    lines = [ln.strip() for ln in table_str.strip().split('\n') if ln.strip()]
    
    print("\nDebug: Input lines after stripping:")
    for line in lines:
        print(f"'{line}'")

    # First line should be the header row
    headers = lines[0].split()
    print("\nDebug: Headers found:", headers)
    
    # The numeric headers are everything after first column
    numeric_headers = headers[:]  # Changed: take all headers
    print("Debug: Numeric headers:", numeric_headers)

    data = {}

    # Process each subsequent line as a row
    for row_line in lines[1:]:
        parts = row_line.split()
        row_person = parts[0]
        
        if row_person not in valid_people:
            print(f"Debug: Skipping invalid row person: {row_person}")
            continue

        # The remaining parts are numeric strings
        row_values = parts[1:]
        print(f"\nDebug: Processing row for {row_person}")
        print(f"Debug: Values found: {row_values}")

        # Build an inner dictionary
        row_dict = {}
        for col_header, val_str in zip(numeric_headers, row_values):
            if col_header in valid_people:
                row_dict[col_header] = int(val_str)
                print(f"Debug: Added {row_person} -> {col_header} = {val_str}")

        data[row_person] = row_dict

    print("\nDebug: Final data structure:")
    for person in valid_people:
        if person in data:
            print(f"{person}: {data[person]}")

    return data

def create_graph_variation(data, index, output_filename):
    """
    Creates a graph with triangle+center layout and very small nodes
    """
    dot = graphviz.Digraph("PeopleGraph", format="png", engine='neato')
    
    # Much larger canvas and higher DPI for better resolution
    dot.attr(size='20,20')  # Dramatically increased canvas size
    dot.attr(dpi='400')     # Higher DPI for sharper rendering
    
    valid_people = ["Daffy", "Ernie", "Ivan", "Jason"]

    # Dramatically smaller nodes
    dot.attr('node', 
            shape='circle', 
            style='filled', 
            fillcolor='lightblue',
            width='0.04',    # Extremely small nodes
            height='0.02',   # Keep aspect ratio square
            fontsize='14')   # Readable font size for node labels
    
    # Manual triangle positioning with much larger radius
    dot.node("Jason", "Jason", pos="0,0!")
    
    # Much larger radius for triangle
    radius = 3.0  # Halved from 6.0 to make edges shorter
    angles = [0, 120, 240]
    positions = [(radius * math.cos(math.radians(a)), 
                 radius * math.sin(math.radians(a))) 
                for a in angles]
    
    for person, pos in zip(["Daffy", "Ernie", "Ivan"], positions):
        dot.node(person, person, pos=f"{pos[0]},{pos[1]}!")

    # Edge attributes for better label placement
    dot.attr('edge', 
            fontsize='30',        # Larger font for edge labels
            fontname='Arial',
            labelangle='45',
            penwidth='0.5')       # Thinner edges

    for src in valid_people:
        for dst in valid_people:
            val = data[src].get(dst, None)
            if val is not None:
                label_str = f"+{val}" if val >= 0 else str(val)
                # Determine label color based on value
                if val == 0:
                    label_color = '#808080'  # grey
                elif val < 0:
                    label_color = '#FF9999'  # light red
                else:
                    label_color = '#90EE90'  # light green
                
                # Fixed distances: 2.0 for self-edges, 1.2 for non-self edges
                if src == dst:
                    label_dist = 2.0
                else:
                    label_dist = 2.2  # Increased from 0.5 to move labels further from source
                
                dot.edge(src, dst, 
                        label='',
                        labeldistance=str(label_dist),
                        fontcolor=label_color,
                        headlabel=label_str,
                        taillabel='',
                        labelfloat='false',
                        labelangle='0')

    # Generate description
    desc = f"#{index:03d} - Large Triangle (r={radius:.1f}), Tiny Nodes (w=0.02)"
    
    # Render and annotate
    dot.render(output_filename, cleanup=True)
    
    img = Image.open(output_filename + '.png')
    draw = ImageDraw.Draw(img)
    try:
        font = ImageFont.truetype("DejaVuSans.ttf", 20)
    except:
        font = ImageFont.load_default()
    
    text_bbox = draw.textbbox((10, img.height - 40), desc, font=font)
    draw.rectangle([text_bbox[0]-5, text_bbox[1]-5, text_bbox[2]+5, text_bbox[3]+5], 
                  fill='white')
    draw.text((10, img.height - 40), desc, fill='black', font=font)
    
    img.save(output_filename + '.png')
    
    return desc

def main():
    data = parse_table(TABLE_TEXT)
    
    # Create output directory if it doesn't exist
    os.makedirs("graph_variations", exist_ok=True)
    
    # Generate multiple variations
    for i in range(5):
        output_filename = f"graph_variations/graph_{i:03d}"
        desc = create_graph_variation(data, i, output_filename)
        print(f"Generated: {desc}")

if __name__ == "__main__":
    main()
