import re

datfiles = ["sanc1.txt", "sanc2.txt", "sanc3.txt", "sanc4.txt", "sanc5.txt", "sanc6.txt", "sanc7.txt", "sanc8.txt", "sanc9.txt", "sanc10.txt"]
mods = {
	'+# to maximum Life' : [
		{'max' : 9, 'lvl' : 1, 'weight' : 10000}, 
		{'max' : 19, 'lvl' : 5, 'weight' : 10000},
		{'max' : 29, 'lvl' : 11, 'weight' : 10000},
		{'max' : 39, 'lvl' : 18, 'weight' : 10000},
		{'max' : 49, 'lvl' : 24, 'weight' : 10000},
		{'max' : 59, 'lvl' : 30, 'weight' : 10000},
		{'max' : 69, 'lvl' : 36, 'weight' : 10000},
		{'max' : 79, 'lvl' : 44, 'weight' : 10000},
		{'max' : 89, 'lvl' : 54, 'weight' : 10000},
		{'max' : 99, 'lvl' : 64, 'weight' : 10000},
		{'max' : 109, 'lvl' : 73, 'weight' : 10000},
		{'max' : 119, 'lvl' : 81, 'weight' : 10000},
		{'max' : 129, 'lvl' : 86, 'weight' : 10000}
	],
	'Reflects # Physical Damage to Melee Attackers' : [
		{'max' : 4, 'lvl' : 1, 'weight' : 1000},
		{'max' : 10, 'lvl' : 10, 'weight' : 1000},
		{'max' : 24, 'lvl' : 20, 'weight' : 1000},
		{'max' : 50, 'lvl' : 35, 'weight' : 1000}
	],
	'+# to Strength' : [
		{'max' : 12, 'lvl' : 1, 'weight' : 1000},
		{'max' : 17, 'lvl' : 11, 'weight' : 1000},
		{'max' : 22, 'lvl' : 22, 'weight' : 1000},
		{'max' : 27, 'lvl' : 33, 'weight' : 1000},
		{'max' : 32, 'lvl' : 44, 'weight' : 1000},
		{'max' : 37, 'lvl' : 55, 'weight' : 1000},
		{'max' : 42, 'lvl' : 66, 'weight' : 1000},
		{'max' : 50, 'lvl' : 74, 'weight' : 1000},
		{'max' : 55, 'lvl' : 82, 'weight' : 1000}
	],
	'Regenerate # Life per second' : [
		{'max' : 2, 'lvl' : 1, 'weight' : 1000},
		{'max' : 4, 'lvl' : 18, 'weight' : 1000},
		{'max' : 7, 'lvl' : 30, 'weight' : 1000},
		{'max' : 10, 'lvl' : 44, 'weight' : 1000},
		{'max' : 13, 'lvl' : 59, 'weight' : 1000},
		{'max' : 16, 'lvl' : 78, 'weight' : 1000},
		{'max' : 20, 'lvl' : 83, 'weight' : 1000},
		{'max' : 25, 'lvl' : 86, 'weight' : 1000}
	],
	'+#% to Fire Resistance' : [
		{'max' : 11, 'lvl' : 1, 'weight' : 1000},
		{'max' : 17, 'lvl' : 12, 'weight' : 1000},
		{'max' : 23, 'lvl' : 24, 'weight' : 1000},
		{'max' : 29, 'lvl' : 36, 'weight' : 1000},
		{'max' : 35, 'lvl' : 48, 'weight' : 1000},
		{'max' : 41, 'lvl' : 60, 'weight' : 1000},
		{'max' : 45, 'lvl' : 72, 'weight' : 1000},
		{'max' : 48, 'lvl' : 84, 'weight' : 1000}
	],
	'+#% to Cold Resistance' : [
		{'max' : 11, 'lvl' : 1, 'weight' : 1000},
		{'max' : 17, 'lvl' : 14, 'weight' : 1000},
		{'max' : 23, 'lvl' : 26, 'weight' : 1000},
		{'max' : 29, 'lvl' : 38, 'weight' : 1000},
		{'max' : 35, 'lvl' : 50, 'weight' : 1000},
		{'max' : 41, 'lvl' : 60, 'weight' : 1000},
		{'max' : 45, 'lvl' : 72, 'weight' : 1000},
		{'max' : 48, 'lvl' : 84, 'weight' : 1000}
	],
	'+#% to Lightning Resistance' : [
		{'max' : 11, 'lvl' : 1, 'weight' : 1000},
		{'max' : 17, 'lvl' : 13, 'weight' : 1000},
		{'max' : 23, 'lvl' : 25, 'weight' : 1000},
		{'max' : 29, 'lvl' : 37, 'weight' : 1000},
		{'max' : 35, 'lvl' : 49, 'weight' : 1000},
		{'max' : 41, 'lvl' : 60, 'weight' : 1000},
		{'max' : 45, 'lvl' : 72, 'weight' : 1000},
		{'max' : 48, 'lvl' : 84, 'weight' : 1000}
	],
	'+#% to Chaos Resistance' : [
		{'max' : 10, 'lvl' : 16, 'weight' : 250},
		{'max' : 15, 'lvl' : 30, 'weight' : 250},
		{'max' : 20, 'lvl' : 44, 'weight' : 250},
		{'max' : 25, 'lvl' : 56, 'weight' : 250},
		{'max' : 30, 'lvl' : 65, 'weight' : 250},
		{'max' : 35, 'lvl' : 81, 'weight' : 250}
	],
	'#% increased Stun and Block Recovery' : [
		{'max' : 13, 'lvl' : 1, 'weight' : 1000},
		{'max' : 16, 'lvl' : 17, 'weight' : 1000},
		{'max' : 19, 'lvl' : 28, 'weight' : 1000},
		{'max' : 22, 'lvl' : 42, 'weight' : 1000},
		{'max' : 25, 'lvl' : 56, 'weight' : 1000},
		{'max' : 28, 'lvl' : 79, 'weight' : 1000}
	],
	'#% reduced Attribute Requirements' : [
		{'max' : 18, 'lvl' : 36, 'weight' : 850},
		{'max' : 32, 'lvl' : 60, 'weight' : 850}
	],
	'#% additional Physical Damage Reduction' : [
		{'max' : 2, 'lvl' : 25, 'weight' : 2000},
		{'max' : 4, 'lvl' : 85, 'weight' : 1000}
	]
}
patterns = dict()
for s in mods:
	patterns[s] = re.compile("^" + s.replace("+","\\+").replace("#","(\\d+(\\.\\d+)?)") + "$", re.IGNORECASE)
	for d in mods[s]:
		d['count'] = 0
count = 0
remainders = dict()
for s in datfiles:
	with open(s) as f:
		for line in f:
			if "Astral Plate" in line:
				count += 1
			for ps in patterns:
				m = patterns[ps].match(line)
				if m:
					n = float(m.group(1))
					if "maximum Life" in ps and n >= 10:
						remainders[n%10] = remainders.get(n%10, 0) + 1
					for mod in mods[ps]:
						if n <= mod['max']:
							mod['count'] += 1
							break
					break
print(count, "items")
with open('dat.csv', 'w') as f:
	for s in mods:
		for d in mods[s]:
			print(s, d['lvl'], d['count'], sep=',', file=f)
	for n in sorted(remainders):
		print(n, remainders[n], sep=',', file=f)