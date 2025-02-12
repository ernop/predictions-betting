import matplotlib.pyplot as plt
import networkx as nx
import numpy as np
import re

def parse_input(input_text):
    lines = input_text.strip().split("\n")

    # Get headers from the first line, excluding the first empty column
    headers = [h for h in re.split(r'\s+', lines[0].strip()) if h]

    # Extract row labels and numeric matrix values
    matrix_data = []
    for line in lines[1:]:
        parts = [p for p in re.split(r'\s+', line.strip()) if p]
        # Skip the row label (first column) and convert rest to integers
        matrix_data.append([int(x) for x in parts[1:]])

    matrix = np.array(matrix_data, dtype=int)

    print(f"Headers: {headers}")
    print(f"Matrix shape: {matrix.shape}")
    print(f"Matrix: \n{matrix}")

    return headers, matrix

def generate_graph(headers, matrix, output_file="graph.png"):
    # Create directed graph
    G = nx.DiGraph()

    # Add edges with weights
    num_rows, num_cols = matrix.shape
    for i in range(num_rows):
        for j in range(num_cols):
            if matrix[i, j] != 0:  # Ignore zero-weight edges
                src = headers[0:num_rows][i]
                dest = headers[j]
                label = f"+{matrix[i, j]}" if matrix[i, j] > 0 else str(matrix[i, j])
                G.add_edge(src, dest, weight=label)

    # Let graphviz handle the layout
    plt.figure(figsize=(12, 12))
    pos = nx.nx_agraph.graphviz_layout(G, prog='neato')
    
    # Draw the complete graph
    nx.draw(G, pos, with_labels=True, node_color='lightblue', 
           node_size=3000, font_size=10, font_weight='bold')
    
    # Add edge labels
    edge_labels = nx.get_edge_attributes(G, 'weight')
    nx.draw_networkx_edge_labels(G, pos, edge_labels)

    plt.axis('off')
    plt.savefig(output_file, bbox_inches='tight')
    plt.close()

def main():
    input_text = """
        \t\t\t\t\t\tDaffy\tErnie\tIvan\tJason
        Daffy\t-3\t-3\t-4\t4
        Ernie\t3\t-1\t0\t6
        Ivan\t0\t-2\t2\t-5
        Jason\t1\t6\t3\t-5
    """
    headers, matrix = parse_input(input_text)
    generate_graph(headers, matrix)
    print("Graph image saved as 'graph.png'")

if __name__ == "__main__":
    main()
