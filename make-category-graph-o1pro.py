#!/usr/bin/env python3

import graphviz
import random
from PIL import Image, ImageDraw, ImageFont
import os
import math

# Multiline string of the data exactly as given:
TABLE_TEXT = """
        Daffy   Ernie   Ivan    Jason
Daffy  -3.2	-3.2	-4.4	3.8	-0.8	-0.8
Ernie 3.0	-1.0	0.0	5.7	-0.7	-1.4
Ivan -0.4	-2.2	1.6	-4.9	-1.3	-1.7
Jason 0.6	6.4	2.9	-4.6	2.9	1.3
"""

TABLE_LABEL = "Prediction FullContract net score by subject, 2024"

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
                row_dict[col_header] = float(val_str)
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
            fontsize='20',        # Larger font for edge labels
            fontname='Arial Bold',  # Changed to bold font
            penwidth='0.3')       # Thinner edges

    # Define port positions for self-edges with slight offsets
    self_edge_ports = {
        "Daffy": ("ne", "se"),     # right side: northeast to southeast
        "Ernie": ("nw", "sw"),     # left side: northwest to southwest
        "Ivan": ("nw", "sw"),      # left side: northwest to southwest
        "Jason": ("nw", "sw")      # left side: northwest to southwest
    }

    for src in valid_people:
        for dst in valid_people:
            val = data[src].get(dst, None)
            if val is not None:
                label_str = f"+{val}" if val >= 0 else str(val)
                if val == 0:
                    label_color = '#808080'  # grey
                elif val < 0:
                    label_color = '#FF9999'  # light red
                else:
                    label_color = '#90EE90'  # light green
                label_angle = '15'
                if src == dst:
                    # Self-edge: use specific ports for each vertex
                    label_dist = 2.2
                    tailport, headport = self_edge_ports[src]
                    dot.edge(src, dst, 
                            label='',
                            labeldistance=str(label_dist),
                            fontcolor=label_color,
                            headlabel='',
                            taillabel=label_str,
                            labelfloat='false',
                            labelangle=label_angle,
                            dir='',              # Removed double-headed arrow
                            minlen='1',
                            weight='1',
                            tailport=tailport,
                            headport=headport,
                            constraint='false')
                else:
                    # Regular edge
                    label_dist = 3.6
                    
                    dot.edge(src, dst, 
                            label='',
                            labeldistance=str(label_dist),
                            fontcolor=label_color,
                            headlabel='',
                            taillabel=label_str,
                            labelfloat='false',
                            labelangle=label_angle)

    # Generate description
    desc = f"#{index:03d} - Large Triangle (r={radius:.1f}), Tiny Nodes (w=0.02)"
    desc = TABLE_LABEL
    
    # Render and annotate
    dot.render(output_filename, cleanup=True)
    
    img = Image.open(output_filename + '.png')
    
    # Create a new taller image with white background
    title_height = 200  # Increased space for larger title
    new_img = Image.new('RGB', (img.width, img.height + title_height), 'white')
    
    # Paste original graph below the title space
    new_img.paste(img, (0, title_height))
    
    # Add title text
    draw = ImageDraw.Draw(new_img)
    try:
        font = ImageFont.truetype("DejaVuSans.ttf", 96)  # Increased from 36 to 108
    except:
        font = ImageFont.load_default()
    
    # Draw text in the empty space
    draw.text((20, 20), desc, fill='black', font=font)
    
    new_img.save(output_filename + '.png')
    
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
