#!/usr/bin/python3

import os
minimize = False
best_scores = {}
file_scores = {}
scores = 'scores100/'
latest_file = None
latest_time = None

for file in os.listdir(scores):
    if not file.endswith('.txt'): continue
    mod_time = os.path.getmtime(scores+file)
    if latest_file == None or mod_time > latest_time:
        latest_time = mod_time
        latest_file = file
    file_scores[file] = {}
    with open(scores + file) as f:
        lines = f.readlines()
    for line in lines:
        if not "Seed = " in line: continue
        l = line[line.index("Seed = "):]
        l = l.replace('= ', '').replace(', ', ' ').split(' ')
        seed = int(l[1])
        score = float(l[3])
        file_scores[file][seed] = score
        if score == -1: continue
        if not seed in best_scores: best_scores[seed] = score
        if minimize: best_scores[seed] = min(best_scores[seed], score)
        else: best_scores[seed] = max(best_scores[seed], score)

keys = sorted(list(best_scores.keys()))
print('   '.join(str(k)+':'+str(best_scores[k]) for k in keys))

rel_scores = []
for f in sorted(file_scores):
    if len(file_scores[f]) == 0: continue
    score = 0
    test_scores = []
    for seed in file_scores[f]:
        if file_scores[f][seed] == -1: continue
        if minimize: rel_score = best_scores[seed] / file_scores[f][seed]
        else: rel_score = file_scores[f][seed] / best_scores[seed]
        score += rel_score
        test_scores.append(rel_score)
        if f == latest_file: rel_scores.append([seed, rel_score])
    print(f, score, '...', score * len(best_scores) / len(file_scores[f]), ' - ', sum(sorted(test_scores)[len(test_scores)//10:]))

rel_scores.sort(key=lambda x:x[1])
print('FAILS at ' + latest_file)
for rel in rel_scores[:10]:
    print(f'{rel[0]}: {rel[1]}')
