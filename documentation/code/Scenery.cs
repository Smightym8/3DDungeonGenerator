var direction = (room.CentrePoint - transform.position).normalized;
instantiatedScenery.transform.rotation = Quaternion.LookRotation(direction);